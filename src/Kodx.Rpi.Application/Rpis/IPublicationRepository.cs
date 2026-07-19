using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IPublicationRepository
{
    /// <summary>Substitui todas as publicações já persistidas para a edição (idempotente em reprocessamentos).</summary>
    Task ReplaceForEditionAsync(int rpiEditionId, IReadOnlyCollection<Publication> publications, CancellationToken cancellationToken);
}
