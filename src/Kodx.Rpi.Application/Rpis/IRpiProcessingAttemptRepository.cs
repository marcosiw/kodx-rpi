using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IRpiProcessingAttemptRepository
{
    Task AddAsync(RpiProcessingAttempt attempt, CancellationToken cancellationToken);
}
