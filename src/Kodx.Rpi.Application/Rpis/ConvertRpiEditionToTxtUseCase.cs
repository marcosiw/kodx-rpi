using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public sealed class ConvertRpiEditionToTxtUseCase(
    IRpiFileStorage fileStorage,
    IPdfTextExtractor pdfTextExtractor,
    IRpiEditionRepository editionRepository,
    IRpiProcessingAttemptRepository attemptRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Retorna se a conversão teve sucesso, para quem chamou (ex: o controller, ao enfileirar) saber se deve encadear o upload pro Blob Storage.</summary>
    public async Task<bool> ExecuteAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
    {
        var edition = await editionRepository.FindAsync(tipo, edicao, cancellationToken);
        if (edition is null)
        {
            // Não há RpiEdition (e portanto nenhum PDF baixado) para registrar a tentativa — nada a fazer.
            return false;
        }

        var startedAt = timeProvider.GetUtcNow();
        bool succeeded;

        try
        {
            var pdfPath = fileStorage.GetPdfPath(tipo, edicao);

            // PdfPig lança se o arquivo estiver corrompido/ilegível — é a validação de
            // integridade do PDF, feita aqui com a mesma lib usada pra extrair o texto.
            var text = pdfTextExtractor.ExtractText(pdfPath);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Texto extraído do PDF está vazio.");
            }

            await fileStorage.SaveTxtAsync(tipo, edicao, text, cancellationToken);

            var attempt = RpiProcessingAttempt.Success(edition.Id, ProcessingStage.ConvertToTxt, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
            succeeded = true;
        }
        catch (Exception ex)
        {
            var attempt = RpiProcessingAttempt.Failure(edition.Id, ProcessingStage.ConvertToTxt, ex.Message, startedAt, timeProvider.GetUtcNow());
            await attemptRepository.AddAsync(attempt, cancellationToken);
            succeeded = false;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return succeeded;
    }
}
