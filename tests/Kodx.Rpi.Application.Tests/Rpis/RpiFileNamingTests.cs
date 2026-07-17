using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Tests.Rpis;

public sealed class RpiFileNamingTests
{
    [Theory]
    [InlineData(RpiTipo.Patentes, "Patentes2891.pdf")]
    [InlineData(RpiTipo.Marcas, "Marcas2891.pdf")]
    [InlineData(RpiTipo.DesenhosIndustriais, "Desenhos_Industriais2891.pdf")]
    [InlineData(RpiTipo.TopografiaCircuitos, "Topografia_de_circuto_Integrado2891.pdf")]
    public void PdfFileName_segue_a_convencao_do_inpi(RpiTipo tipo, string expected)
    {
        Assert.Equal(expected, RpiFileNaming.PdfFileName(tipo, 2891));
    }
}
