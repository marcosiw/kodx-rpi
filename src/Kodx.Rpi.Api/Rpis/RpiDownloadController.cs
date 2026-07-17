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
    /// for informada, calcula a edição corrente a partir do anchor configurado.
    /// </summary>
    [HttpPost("{tipo}/download/{edicao?}")]
    public IActionResult Download(RpiTipo tipo, int? edicao)
    {
        taskQueue.Enqueue(async (services, cancellationToken) =>
        {
            var useCase = services.GetRequiredService<DownloadRpiEditionUseCase>();
            await useCase.ExecuteAsync(tipo, edicao, cancellationToken);
        });

        return Accepted(new { tipo, edicao });
    }
}
