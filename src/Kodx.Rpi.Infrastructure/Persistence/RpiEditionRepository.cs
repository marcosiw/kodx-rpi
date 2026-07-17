using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.EntityFrameworkCore;

namespace Kodx.Rpi.Infrastructure.Persistence;

public sealed class RpiEditionRepository(KodxRpiDbContext dbContext) : IRpiEditionRepository
{
    public Task<RpiEdition?> FindAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
        dbContext.RpiEditions.SingleOrDefaultAsync(e => e.Tipo == tipo && e.Edicao == edicao, cancellationToken);

    public async Task AddAsync(RpiEdition edition, CancellationToken cancellationToken) =>
        await dbContext.RpiEditions.AddAsync(edition, cancellationToken);
}
