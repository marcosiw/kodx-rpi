using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public sealed class DownloadRpiEditionUseCase(
    IRpiDownloader downloader,
    IRpiFileStorage fileStorage,
    IRpiEditionRepository editionRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork,
    RpiEditionCalculator editionCalculator,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(RpiTipo tipo, int? edicao, CancellationToken cancellationToken)
    {
        var resolvedEdicao = edicao ?? editionCalculator.CurrentEdition();

        var edition = await editionRepository.FindAsync(tipo, resolvedEdicao, cancellationToken);
        if (edition is null)
        {
            var publicationDate = editionCalculator.PublicationDateFor(resolvedEdicao);
            edition = new RpiEdition(
                resolvedEdicao,
                tipo,
                new DateTimeOffset(publicationDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

            await editionRepository.AddAsync(edition, cancellationToken);
            // Precisa persistir agora para obter o Id gerado pelo banco antes de gravar a tentativa (FK).
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var startedAt = timeProvider.GetUtcNow();

        try
        {
            var pdfBytes = await downloader.DownloadAsync(tipo, resolvedEdicao, cancellationToken);
            ValidatePdf(pdfBytes);
            await fileStorage.SavePdfAsync(tipo, resolvedEdicao, pdfBytes, cancellationToken);

            var attempt = RpiProcessingAttempt.Success(edition.Id, ProcessingStage.Download, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
        }
        catch (Exception ex)
        {
            // Qualquer falha no download/validação/gravação vira uma tentativa registrada,
            // nunca uma exceção não tratada — é exatamente o histórico que a spec pede.
            var attempt = RpiProcessingAttempt.Failure(edition.Id, ProcessingStage.Download, ex.Message, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ValidatePdf(byte[] content)
    {
        if (content.Length == 0)
        {
            throw new RpiDownloadException("Arquivo baixado está vazio.");
        }

        if (content.Length < 4 || content[0] != (byte)'%' || content[1] != (byte)'P' || content[2] != (byte)'D' || content[3] != (byte)'F')
        {
            throw new RpiDownloadException("Conteúdo baixado não parece ser um PDF válido (assinatura %PDF ausente).");
        }
    }
}
