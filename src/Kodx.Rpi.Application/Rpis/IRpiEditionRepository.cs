using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IRpiEditionRepository
{
    Task<RpiEdition?> FindAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken);

    Task AddAsync(RpiEdition edition, CancellationToken cancellationToken);
}
