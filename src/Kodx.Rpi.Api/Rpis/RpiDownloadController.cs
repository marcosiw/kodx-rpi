using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.BackgroundProcessing;
using Microsoft.AspNetCore.Mvc;

namespace Kodx.Rpi.Api.Rpis;

[ApiController]
[Route("rpis")]
public sealed class RpiDownloadController(IBackgroundTaskQueue taskQueue) : ControllerBase
{
    /// <summary>
    /// Aciona o download em background de uma edição da RPI. Se <paramref name="edicao"/> não
    /// for informada, resolve a edição mais recente pelo calendário oficial do INPI. Em caso
    /// de sucesso, encadeia automaticamente a conversão para TXT e, em seguida, o upload do
    /// PDF/TXT pro Blob Storage e a extração das publicações — tudo na mesma fila. Upload e
    /// extração são independentes entre si (cada um só depende da conversão ter funcionado).
    /// </summary>
    [HttpPost("{tipo}/download/{edicao?}")]
    public IActionResult Download(RpiTipo tipo, int? edicao)
    {
        taskQueue.Enqueue(async (services, cancellationToken) =>
        {
            var downloadUseCase = services.GetRequiredService<DownloadRpiEditionUseCase>();
            var downloadResult = await downloadUseCase.ExecuteAsync(tipo, edicao, cancellationToken);

            if (!downloadResult.Success)
            {
                return;
            }

            var convertUseCase = services.GetRequiredService<ConvertRpiEditionToTxtUseCase>();
            var converted = await convertUseCase.ExecuteAsync(tipo, downloadResult.Edicao, cancellationToken);

            if (!converted)
            {
                return;
            }

            var uploadUseCase = services.GetRequiredService<UploadRpiEditionToBlobUseCase>();
            await uploadUseCase.ExecuteAsync(tipo, downloadResult.Edicao, cancellationToken);

            var extractUseCase = services.GetRequiredService<ExtractRpiPublicationsUseCase>();
            await extractUseCase.ExecuteAsync(tipo, downloadResult.Edicao, cancellationToken);
        });

        return Accepted(new { tipo, edicao });
    }
}
