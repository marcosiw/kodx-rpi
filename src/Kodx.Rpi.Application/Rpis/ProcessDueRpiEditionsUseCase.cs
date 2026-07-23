using Kodx.Rpi.Domain.Rpis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kodx.Rpi.Application.Rpis;

/// <summary>
/// Chamada uma vez por tick do worker periódico (fase 9, ver ai/context.md): resolve a edição
/// mais recente da RPI e enfileira <see cref="RunRpiPipelineUseCase"/> pra cada um dos 8 tipos
/// que ainda não têm o pipeline concluído com sucesso pra essa edição.
/// </summary>
public sealed class ProcessDueRpiEditionsUseCase(
    IRpiCalendar calendar,
    RpiEditionCalculator editionCalculator,
    IRpiEditionRepository editionRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IRpiPublicationExtractor extractor,
    IBackgroundTaskQueue taskQueue,
    ILogger<ProcessDueRpiEditionsUseCase> logger)
{
    private static readonly RpiTipo[] AllTipos = Enum.GetValues<RpiTipo>();

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var mostRecent = await calendar.GetMostRecentEditionAsync(cancellationToken);
        var edicao = mostRecent?.Edicao ?? editionCalculator.CurrentEdition();

        foreach (var tipo in await GetPendingTiposAsync(edicao, cancellationToken))
        {
            logger.LogInformation("Worker enfileirando pipeline da RPI {Edicao}/{Tipo}.", edicao, tipo);
            taskQueue.Enqueue((services, ct) =>
                services.GetRequiredService<RunRpiPipelineUseCase>().ExecuteAsync(tipo, edicao, ct));
        }
    }

    /// <summary>Decisão de "o que falta processar" isolada de <see cref="ExecuteAsync"/> pra ser testável sem precisar inspecionar o closure opaco enfileirado em <see cref="IBackgroundTaskQueue"/>.</summary>
    public async Task<IReadOnlyList<RpiTipo>> GetPendingTiposAsync(int edicao, CancellationToken cancellationToken)
    {
        var pending = new List<RpiTipo>();

        foreach (var tipo in AllTipos)
        {
            if (!await IsAlreadyProcessedAsync(tipo, edicao, cancellationToken))
            {
                pending.Add(tipo);
            }
        }

        return pending;
    }

    private async Task<bool> IsAlreadyProcessedAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
    {
        var edition = await editionRepository.FindAsync(tipo, edicao, cancellationToken);
        if (edition is null)
        {
            return false;
        }

        var attempts = await attemptRepository.ListForEditionAsync(edition.Id, cancellationToken);
        var finalStage = extractor.IsSupported(tipo) ? ProcessingStage.ExtractPublications : ProcessingStage.UploadBlob;

        return attempts.Any(a => a.Stage == finalStage && a.Status == ProcessingStatus.Success);
    }
}
