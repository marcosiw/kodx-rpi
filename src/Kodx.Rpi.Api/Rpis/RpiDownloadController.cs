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
    /// de sucesso, encadeia automaticamente a conversão para TXT na mesma fila.
    /// </summary>
    [HttpPost("{tipo}/download/{edicao?}")]
    public IActionResult Download(RpiTipo tipo, int? edicao)
    {
        taskQueue.Enqueue(async (services, cancellationToken) =>
        {
            var downloadUseCase = services.GetRequiredService<DownloadRpiEditionUseCase>();
            var result = await downloadUseCase.ExecuteAsync(tipo, edicao, cancellationToken);

            if (result.Success)
            {
                var convertUseCase = services.GetRequiredService<ConvertRpiEditionToTxtUseCase>();
                await convertUseCase.ExecuteAsync(tipo, result.Edicao, cancellationToken);
            }
        });

        return Accepted(new { tipo, edicao });
    }
}
