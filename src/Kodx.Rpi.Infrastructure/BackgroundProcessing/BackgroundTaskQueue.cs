using System.Threading.Channels;
using Kodx.Rpi.Application.Rpis;

namespace Kodx.Rpi.Infrastructure.BackgroundProcessing;

/// <summary>Fila de jobs em memória (padrão "Queued background tasks" documentado pelo ASP.NET Core). Perde a fila se o processo reiniciar.</summary>
public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel =
        Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem) =>
        _channel.Writer.TryWrite(workItem);

    public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken) =>
        await _channel.Reader.ReadAsync(cancellationToken);
}
