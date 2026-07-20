using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public sealed class ExtractRpiPublicationsUseCase(
    IRpiFileStorage fileStorage,
    IRpiPublicationExtractor extractor,
    IRpiEditionRepository editionRepository,
    IPublicationRepository publicationRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
    {
        if (!extractor.IsSupported(tipo))
        {
            // Sem regras de extração cadastradas pra este tipo (mesma cobertura do legado hoje
            // em produção) — nem chega a registrar uma tentativa, não é uma falha.
            return;
        }

        var edition = await editionRepository.FindAsync(tipo, edicao, cancellationToken);
        if (edition is null)
        {
            // Não há RpiEdition (e portanto nenhum TXT convertido) para registrar a tentativa — nada a fazer.
            return;
        }

        var startedAt = timeProvider.GetUtcNow();

        try
        {
            var texto = await fileStorage.ReadTxtAsync(tipo, edicao, cancellationToken);
            var extracted = extractor.Extract(tipo, texto);
            var publications = extracted.Select(e => new Publication(edition.Id, e.Numero, e.Payload)).ToList();

            await publicationRepository.ReplaceForEditionAsync(edition.Id, publications, cancellationToken);

            var attempt = RpiProcessingAttempt.Success(edition.Id, ProcessingStage.ExtractPublications, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
        }
        catch (Exception ex)
        {
            var attempt = RpiProcessingAttempt.Failure(edition.Id, ProcessingStage.ExtractPublications, ex.Message, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
