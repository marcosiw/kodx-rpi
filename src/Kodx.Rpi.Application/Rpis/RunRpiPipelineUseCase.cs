using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

/// <summary>
/// Orquestra a cadeia download → conversão → upload → extração pra uma RPI, reaproveitada tanto
/// pelo trigger via gRPC (<c>RpiGrpcService.TriggerDownload</c>) quanto pelo worker periódico
/// (fase 9, ver ai/context.md). Cada etapa já grava sua própria <see cref="RpiProcessingAttempt"/>;
/// esta classe só decide se avança pra próxima etapa, sem lançar em caso de falha.
/// </summary>
public sealed class RunRpiPipelineUseCase(
    DownloadRpiEditionUseCase downloadUseCase,
    ConvertRpiEditionToTxtUseCase convertUseCase,
    UploadRpiEditionToBlobUseCase uploadUseCase,
    ExtractRpiPublicationsUseCase extractUseCase)
{
    public async Task ExecuteAsync(RpiTipo tipo, int? edicao, CancellationToken cancellationToken)
    {
        var downloadResult = await downloadUseCase.ExecuteAsync(tipo, edicao, cancellationToken);
        if (!downloadResult.Success)
        {
            return;
        }

        var converted = await convertUseCase.ExecuteAsync(tipo, downloadResult.Edicao, cancellationToken);
        if (!converted)
        {
            return;
        }

        await uploadUseCase.ExecuteAsync(tipo, downloadResult.Edicao, cancellationToken);
        await extractUseCase.ExecuteAsync(tipo, downloadResult.Edicao, cancellationToken);
    }
}
