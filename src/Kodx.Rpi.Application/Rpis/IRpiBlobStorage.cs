using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

/// <summary>Envia PDF/TXT locais para o backup no Blob Storage (Azure), seguindo a convenção de path/tags já usada pelo legado.</summary>
public interface IRpiBlobStorage
{
    Task UploadPdfAsync(RpiTipo tipo, int edicao, string localPdfPath, CancellationToken cancellationToken);

    Task UploadTxtAsync(RpiTipo tipo, int edicao, string localTxtPath, CancellationToken cancellationToken);
}
