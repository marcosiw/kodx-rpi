using Kodx.Rpi.Application.Rpis;

namespace Kodx.Rpi.Api.Tests.Grpc;

/// <summary>
/// Substitui a fila real nos testes: grava os jobs enfileirados sem executá-los, pra não
/// disparar downloads reais do INPI/Blob Storage em background durante os testes.
/// </summary>
public sealed class FakeBackgroundTaskQueue : IBackgroundTaskQueue
{
    public List<Func<IServiceProvider, CancellationToken, Task>> Enqueued { get; } = [];

    public void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem) => Enqueued.Add(workItem);

    public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        throw new OperationCanceledException(cancellationToken);
    }
}
