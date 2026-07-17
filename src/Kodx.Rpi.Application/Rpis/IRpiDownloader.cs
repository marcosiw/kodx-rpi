using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IRpiDownloader
{
    /// <summary>Baixa o PDF da RPI. Lança <see cref="RpiDownloadException"/> em caso de falha.</summary>
    Task<byte[]> DownloadAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken);
}
