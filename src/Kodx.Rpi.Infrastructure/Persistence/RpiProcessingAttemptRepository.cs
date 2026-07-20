using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class RpiProcessingAttemptRepository(KodxRpiDbContext dbContext) : IRpiProcessingAttemptRepository
{
    public async Task AddAsync(RpiProcessingAttempt attempt, CancellationToken cancellationToken) =>
        await dbContext.RpiProcessingAttempts.AddAsync(attempt, cancellationToken);

    public async Task<IReadOnlyList<RpiProcessingAttempt>> ListForEditionAsync(int rpiEditionId, CancellationToken cancellationToken) =>
        await dbContext.RpiProcessingAttempts
            .Where(a => a.RpiEditionId == rpiEditionId)
            .OrderBy(a => a.StartedAt)
            .ToListAsync(cancellationToken);
}
