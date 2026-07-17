using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class RpiProcessingAttemptRepository(KodxRpiDbContext dbContext) : IRpiProcessingAttemptRepository
{
    public async Task AddAsync(RpiProcessingAttempt attempt, CancellationToken cancellationToken) =>
        await dbContext.RpiProcessingAttempts.AddAsync(attempt, cancellationToken);
}
