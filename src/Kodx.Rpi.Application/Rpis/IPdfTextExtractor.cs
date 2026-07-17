namespace Kodx.Rpi.Application.Rpis;

public interface IPdfTextExtractor
{
    /// <summary>Extrai o texto do PDF em <paramref name="pdfPath"/>. Lança se o arquivo estiver corrompido/ilegível — é a validação de integridade do PDF.</summary>
    string ExtractText(string pdfPath);
}
