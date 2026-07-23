using Kodx.Rpi.Application.Rpis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kodx.Rpi.Infrastructure.BackgroundProcessing;

/// <summary>
/// Acorda periodicamente e enfileira o pipeline pras edições/tipos da RPI ainda pendentes (fase
/// 9, ver ai/context.md) — substitui a ideia original de scripts de cronjob externos. A decisão
/// de o que está pendente fica em <see cref="ProcessDueRpiEditionsUseCase"/>; esta classe só cuida
/// do loop e do escopo de DI por tick, no mesmo espírito de <see cref="QueuedHostedService"/>.
/// </summary>
public sealed class RpiWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RpiWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<RpiWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval, timeProvider);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessDueRpiEditionsUseCase>();
                await useCase.ExecuteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falha ao checar edições pendentes da RPI no worker.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
