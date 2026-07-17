using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public sealed class DownloadRpiEditionUseCase(
    IRpiDownloader downloader,
    IRpiFileStorage fileStorage,
    IRpiCalendar calendar,
    IRpiEditionRepository editionRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork,
    RpiEditionCalculator editionCalculator,
    TimeProvider timeProvider)
{
    public async Task<DownloadRpiEditionResult> ExecuteAsync(RpiTipo tipo, int? edicao, CancellationToken cancellationToken)
    {
        int resolvedEdicao;
        DateOnly? knownPublicationDate;

        if (edicao is { } explicitEdicao)
        {
            resolvedEdicao = explicitEdicao;
            knownPublicationDate = await calendar.GetPublicationDateAsync(explicitEdicao, cancellationToken);
        }
        else
        {
            // O calendário oficial já reflete o deslocamento real de feriado; só cai no
            // cálculo por âncora se o INPI estiver fora do ar ou mudar o formato da página.
            var mostRecent = await calendar.GetMostRecentEditionAsync(cancellationToken);
            resolvedEdicao = mostRecent?.Edicao ?? editionCalculator.CurrentEdition();
            knownPublicationDate = mostRecent?.DataPublicacao;
        }

        var edition = await editionRepository.FindAsync(tipo, resolvedEdicao, cancellationToken);
        if (edition is null)
        {
            var publicationDate = knownPublicationDate ?? editionCalculator.PublicationDateFor(resolvedEdicao);
            edition = new RpiEdition(
                resolvedEdicao,
                tipo,
                new DateTimeOffset(publicationDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

            await editionRepository.AddAsync(edition, cancellationToken);
            // Precisa persistir agora para obter o Id gerado pelo banco antes de gravar a tentativa (FK).
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var startedAt = timeProvider.GetUtcNow();
        bool succeeded;

        try
        {
            var pdfBytes = await downloader.DownloadAsync(tipo, resolvedEdicao, cancellationToken);
            ValidatePdf(pdfBytes);
            await fileStorage.SavePdfAsync(tipo, resolvedEdicao, pdfBytes, cancellationToken);

            var attempt = RpiProcessingAttempt.Success(edition.Id, ProcessingStage.Download, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
            succeeded = true;
        }
        catch (Exception ex)
        {
            // Qualquer falha no download/validação/gravação vira uma tentativa registrada,
            // nunca uma exceção não tratada — é exatamente o histórico que a spec pede.
            var attempt = RpiProcessingAttempt.Failure(edition.Id, ProcessingStage.Download, ex.Message, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
            succeeded = false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DownloadRpiEditionResult(succeeded, resolvedEdicao);
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
