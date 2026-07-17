using Kodx.Rpi.Application.Rpis;

namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public string ExtractText(string pdfPath) => Pdf.PdfTextExtractor.ExtractOrdered(pdfPath);
}
