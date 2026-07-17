using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.Extensions.Options;

namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class LocalDiskRpiFileStorage(IOptions<RpiStorageOptions> options) : IRpiFileStorage
{
    public async Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(options.Value.LocalWorkingDirectory, edicao.ToString());
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, RpiFileNaming.PdfFileName(tipo, edicao));
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }
}
