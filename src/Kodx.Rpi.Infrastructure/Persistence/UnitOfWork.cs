using Kodx.Rpi.Application;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class UnitOfWork(KodxRpiDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
