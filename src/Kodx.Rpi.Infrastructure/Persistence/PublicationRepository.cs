using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class PublicationRepository(KodxRpiDbContext dbContext) : IPublicationRepository
{
    public async Task ReplaceForEditionAsync(int rpiEditionId, IReadOnlyCollection<Publication> publications, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Publications
            .Where(p => p.RpiEditionId == rpiEditionId)
            .ToListAsync(cancellationToken);

        dbContext.Publications.RemoveRange(existing);
        await dbContext.Publications.AddRangeAsync(publications, cancellationToken);
    }
}
