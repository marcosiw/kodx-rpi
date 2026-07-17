using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public sealed class UploadRpiEditionToBlobUseCase(
    IRpiFileStorage fileStorage,
    IRpiBlobStorage blobStorage,
    IRpiEditionRepository editionRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
    {
        var edition = await editionRepository.FindAsync(tipo, edicao, cancellationToken);
        if (edition is null)
        {
            // Não há RpiEdition (e portanto nenhum PDF/TXT local) para registrar a tentativa — nada a fazer.
            return;
        }

        var startedAt = timeProvider.GetUtcNow();

        try
        {
            var pdfPath = fileStorage.GetPdfPath(tipo, edicao);
            var txtPath = fileStorage.GetTxtPath(tipo, edicao);

            await blobStorage.UploadPdfAsync(tipo, edicao, pdfPath, cancellationToken);
            await blobStorage.UploadTxtAsync(tipo, edicao, txtPath, cancellationToken);

            var attempt = RpiProcessingAttempt.Success(edition.Id, ProcessingStage.UploadBlob, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
        }
        catch (Exception ex)
        {
            var attempt = RpiProcessingAttempt.Failure(edition.Id, ProcessingStage.UploadBlob, ex.Message, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
