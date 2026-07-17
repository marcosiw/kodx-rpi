using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IRpiFileStorage
{
    /// <summary>Salva o PDF baixado num diretório de trabalho local, para as fases seguintes (conversão, upload) lerem depois.</summary>
    Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken);
}
