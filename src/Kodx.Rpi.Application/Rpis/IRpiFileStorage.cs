using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

public interface IRpiFileStorage
{
    /// <summary>Salva o PDF baixado num diretório de trabalho local, para as fases seguintes (conversão, upload) lerem depois.</summary>
    Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken);

    /// <summary>Caminho local do PDF já baixado (fase de download precisa ter rodado antes).</summary>
    string GetPdfPath(RpiTipo tipo, int edicao);

    /// <summary>Salva o texto convertido no mesmo diretório de trabalho local, para a fase de upload (Blob Storage) ler depois.</summary>
    Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken);

    /// <summary>Caminho local do TXT já convertido (fase de conversão precisa ter rodado antes).</summary>
    string GetTxtPath(RpiTipo tipo, int edicao);
}
