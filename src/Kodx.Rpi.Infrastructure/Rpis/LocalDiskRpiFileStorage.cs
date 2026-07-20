using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Microsoft.Extensions.Options;

namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class LocalDiskRpiFileStorage(IOptions<RpiStorageOptions> options) : IRpiFileStorage
{
    public async Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken)
    {
        var path = GetPdfPath(tipo, edicao);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    public string GetPdfPath(RpiTipo tipo, int edicao) =>
        Path.Combine(EditionDirectory(edicao), RpiFileNaming.PdfFileName(tipo, edicao));

    public async Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken)
    {
        var directory = EditionDirectory(edicao);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, RpiFileNaming.TxtFileName(tipo, edicao));
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }

    public string GetTxtPath(RpiTipo tipo, int edicao) =>
        Path.Combine(EditionDirectory(edicao), RpiFileNaming.TxtFileName(tipo, edicao));

    public Task<string> ReadTxtAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
        File.ReadAllTextAsync(GetTxtPath(tipo, edicao), cancellationToken);

    private string EditionDirectory(int edicao) => Path.Combine(options.Value.LocalWorkingDirectory, edicao.ToString());
}
