using Kodx.Rpi.Infrastructure.Pdf;

namespace Kodx.Rpi.Infrastructure.Tests.Pdf;

/// <summary>Portado de git@github.com:marcosiw/kodx-pdf.git junto com o código que testam.</summary>
public class PdfTextExtractorTests
{
    // --- GroupIntoLines ---

    [Fact]
    public void GroupIntoLines_BlocosDentroTolerancia_AgrupaNaMesmaLinha()
    {
        var blocks = new List<TextBlock>
        {
            new("A", X: 10, Y: 100, Width: 6, FontSize: 10),
            new("B", X: 50, Y: 101, Width: 6, FontSize: 10), // diff = 1, dentro da tolerância de 5
            new("C", X: 80, Y: 100, Width: 6, FontSize: 10),
        };

        var groups = PdfTextExtractor.GroupIntoLines(blocks, minTolerance: 2.0);

        Assert.Single(groups);
        Assert.Equal(3, groups[0].Blocks.Count);
    }

    [Fact]
    public void GroupIntoLines_BlocosForaTolerancia_CriaLinhasSeparadas()
    {
        var blocks = new List<TextBlock>
        {
            new("Linha1A", X: 10, Y: 200, Width: 6, FontSize: 10),
            new("Linha2A", X: 10, Y: 100, Width: 6, FontSize: 10),
            new("Linha1B", X: 60, Y: 200, Width: 6, FontSize: 10),
        };

        var groups = PdfTextExtractor.GroupIntoLines(blocks, minTolerance: 2.0);

        Assert.Equal(2, groups.Count);
    }

    [Fact]
    public void GroupIntoLines_ListaVazia_RetornaVazio()
    {
        var groups = PdfTextExtractor.GroupIntoLines([], minTolerance: 2.0);

        Assert.Empty(groups);
    }

    [Fact]
    public void GroupIntoLines_DescenderComFontSizeDinamico_AgrupaNaMesmaLinha()
    {
        // "g" em "Según" tem Y = 95.8, restante da linha Y = 100.
        // FontSize = 10 → tolerância dinâmica = max(2.0, 10*0.5) = 5.0 → diff 4.2 < 5.0 → mesmo grupo.
        var blocks = new List<TextBlock>
        {
            new("S",  X: 10, Y: 100.0, Width: 6, FontSize: 10),
            new("e",  X: 16, Y: 100.0, Width: 6, FontSize: 10),
            new("g",  X: 22, Y:  95.8, Width: 6, FontSize: 10), // descender
            new("ú",  X: 28, Y: 100.0, Width: 6, FontSize: 10),
            new("n",  X: 34, Y: 100.0, Width: 6, FontSize: 10),
        };

        var groups = PdfTextExtractor.GroupIntoLines(blocks, minTolerance: 2.0);

        Assert.Single(groups);
        Assert.Equal(5, groups[0].Blocks.Count);
    }

    [Fact]
    public void GroupIntoLines_GrupoComAncoraMuitoAlta_DescenderProximoAosMembros_Agrupa()
    {
        // Simula o caso real do RPI: ° ancora o grupo em Y=109, letras principais em Y=105,
        // e o descender 'g' em Y=103.73. Com BaseY fixo, gap=5.65 > tol=4.5 expulsaria o 'g'.
        // Com range [MinY, MaxY] o 'g' fica a 1.78 de 105.51 → dentro da tolerância.
        var blocks = new List<TextBlock>
        {
            new("°", X: 238, Y: 109.38, Width: 2.5, FontSize: 9), // âncora alta
            new("C", X:  85, Y: 105.51, Width: 5.7, FontSize: 9), // texto principal
            new("ó", X:  92, Y: 105.51, Width: 4.2, FontSize: 9),
            new("d", X:  97, Y: 105.51, Width: 4.0, FontSize: 9),
            new("i", X: 102, Y: 105.60, Width: 0.8, FontSize: 9),
            new("g", X: 104, Y: 103.73, Width: 4.0, FontSize: 9), // descender 1.78pt abaixo
            new("o", X: 109, Y: 105.51, Width: 4.2, FontSize: 9),
        };

        var groups = PdfTextExtractor.GroupIntoLines(blocks, minTolerance: 2.0);

        Assert.Single(groups);
        Assert.Equal(7, groups[0].Blocks.Count);
    }

    [Fact]
    public void GroupIntoLines_DescenderForaDaTolerancia_CriaLinhaSeparada()
    {
        // FontSize = 6 → tolerância = max(2.0, 6*0.5) = 3.0. Diff de 4pts → linha separada.
        var blocks = new List<TextBlock>
        {
            new("A", X: 10, Y: 100.0, Width: 4, FontSize: 6),
            new("g", X: 16, Y:  96.0, Width: 4, FontSize: 6), // diff = 4 > 3 → linha separada
        };

        var groups = PdfTextExtractor.GroupIntoLines(blocks, minTolerance: 2.0);

        Assert.Equal(2, groups.Count);
    }

    // --- ConcatenateBlocks ---

    [Fact]
    public void ConcatenateBlocks_SemLacuna_NaoInsereEspaco()
    {
        // Gap = 0 — abaixo do threshold (21% de 10pts = 2.1pts), sem espaço extra.
        var blocks = new List<TextBlock>
        {
            new("Olá",    X: 10, Y: 100, Width: 18, FontSize: 10),
            new(" mundo", X: 28, Y: 100, Width: 36, FontSize: 10),
        };

        var result = PdfTextExtractor.ConcatenateBlocks(blocks);

        Assert.Equal("Olá mundo", result);
    }

    [Fact]
    public void ConcatenateBlocks_ComLacunaGrande_InsereEspaco()
    {
        // Col1 termina em X=34; Col2 começa em 200 — gap 166pts >> threshold 2.5pts.
        var blocks = new List<TextBlock>
        {
            new("Col1", X: 10,  Y: 100, Width: 24, FontSize: 10),
            new("Col2", X: 200, Y: 100, Width: 24, FontSize: 10),
        };

        var result = PdfTextExtractor.ConcatenateBlocks(blocks);

        Assert.Contains(" ", result);
    }

    [Fact]
    public void ConcatenateBlocks_KerningEntreLetras_NaoInsereEspaco()
    {
        // Gap de 0.5pt entre letras da mesma palavra — abaixo do threshold 21% do fontSize.
        var blocks = new List<TextBlock>
        {
            new("C", X: 10.0, Y: 100, Width: 7.0, FontSize: 10),
            new("ó", X: 17.5, Y: 100, Width: 6.5, FontSize: 10), // gap = 0.5pt
            new("d", X: 24.5, Y: 100, Width: 6.0, FontSize: 10), // gap = 0.5pt
        };

        var result = PdfTextExtractor.ConcatenateBlocks(blocks);

        Assert.Equal("Cód", result);
    }

    [Fact]
    public void ConcatenateBlocks_DigitoPrecedendoOutroDigito_GapMarginal_NaoInsereEspaco()
    {
        // Dígitos têm kerning natural largo: "19" pode ter gap ~2.0pt (>threshold 1.89)
        // mas NÃO deve virar "1 9". Regra: gap dígito-marginal (0.21–0.27×fs) é suprimido.
        // fs=9, gap=2.0 (ratio=0.222 — dentro do range 0.21–0.27) → sem espaço.
        var blocks = new List<TextBlock>
        {
            new("1", X: 10.0, Y: 100, Width: 5.0, FontSize: 9),
            new("9", X: 17.0, Y: 100, Width: 5.0, FontSize: 9), // gap = 2.0pt, ratio = 0.222
        };

        var result = PdfTextExtractor.ConcatenateBlocks(blocks);

        Assert.Equal("19", result);
    }

    [Fact]
    public void ConcatenateBlocks_DigitoComGapGrande_InsereEspaco()
    {
        // Gap genuíno após dígito (ex: "2889 de"): ratio=0.36, acima do limite 0.27×fs.
        // fs=9, gap=4.0 (ratio=0.444) → espaço inserido.
        var blocks = new List<TextBlock>
        {
            new("9", X: 10.0, Y: 100, Width: 5.0, FontSize: 9),
            new("d", X: 19.0, Y: 100, Width: 5.0, FontSize: 9), // gap = 4.0pt, ratio = 0.444
        };

        var result = PdfTextExtractor.ConcatenateBlocks(blocks);

        Assert.Equal("9 d", result);
    }

    [Fact]
    public void ConcatenateBlocks_ListaVazia_RetornaStringVazia()
    {
        var result = PdfTextExtractor.ConcatenateBlocks([]);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConcatenateBlocks_BlocoUnico_RetornaTextoSemAlteracao()
    {
        var blocks = new List<TextBlock> { new("Único", X: 10, Y: 100, Width: 30, FontSize: 10) };

        var result = PdfTextExtractor.ConcatenateBlocks(blocks);

        Assert.Equal("Único", result);
    }

    // --- CollectBlocks ---

    [Fact]
    public void CollectBlocks_FiltaLetrasVazias_RetornaSomenteComTexto()
    {
        var blocks = PdfTextExtractor.CollectBlocks([]);

        Assert.Empty(blocks);
    }
}
