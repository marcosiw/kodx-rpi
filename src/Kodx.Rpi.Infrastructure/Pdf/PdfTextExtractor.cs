using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Kodx.Rpi.Infrastructure.Pdf;

/// <summary>Portado de git@github.com:marcosiw/kodx-pdf.git (decisão explícita: copiar em vez de referenciar cross-repo).</summary>
public static class PdfTextExtractor
{
    /// <summary>
    /// Extrai o texto de todas as páginas do PDF respeitando a ordem de leitura
    /// (de cima para baixo, da esquerda para a direita) via coordenadas.
    /// </summary>
    public static string ExtractOrdered(string pdfPath, double minLineTolerance = 2.0)
    {
        using var document = PdfDocument.Open(pdfPath);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            if (page.Number > 1)
                sb.Append($"\n\n--- Página {page.Number} ---\n\n");

            sb.Append(ExtractPageOrdered(page, minLineTolerance));
        }

        return sb.ToString();
    }

    public static string ExtractPageOrdered(Page page, double minLineTolerance = 2.0)
    {
        var blocks = CollectBlocks(page.Letters);
        if (blocks.Count == 0)
            return string.Empty;

        var lines = GroupIntoLines(blocks, minLineTolerance);

        // PDF tem origem no canto inferior esquerdo — Y decrescente = de cima para baixo.
        var ordered = lines
            .SelectMany(l => SplitLineGroup(l, minLineTolerance))
            .OrderByDescending(l => l.BaseY)
            .Select(l => ConcatenateBlocks(l.Blocks.OrderBy(b => b.X).ToList()))
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return string.Join("\n", ordered) + "\n";
    }

    public static List<TextBlock> CollectBlocks(IEnumerable<Letter> letters)
    {
        return letters
            .Where(l => !string.IsNullOrWhiteSpace(l.Value))
            .Select(l => new TextBlock(
                l.Value,
                l.GlyphRectangle.BottomLeft.X,
                l.GlyphRectangle.BottomLeft.Y,
                l.GlyphRectangle.Width,
                l.FontSize)
            {
                // TopY lets MidY use the real glyph centre rather than an estimate.
                // Floating punctuation (curly quotes, ° etc.) whose BottomLeft sits
                // between two visual lines is placed correctly via its MidY.
                TopY = l.GlyphRectangle.TopLeft.Y
            })
            .ToList();
    }

    public static List<LineGroup> GroupIntoLines(List<TextBlock> blocks, double minTolerance = 2.0)
    {
        var groups = new List<LineGroup>();

        // Sort by MidY descending: the vertical centre of each glyph's bounding box.
        // Using MidY (rather than BottomLeft.Y) prevents floating punctuation glyphs
        // (curly quotes, ° etc.) whose bottom sits between two visual lines from
        // acting as cascade bridges that merge consecutive lines into garbled output.
        foreach (var block in blocks.OrderByDescending(b => b.MidY))
        {
            // Cap tolerance by the glyph's actual rendered height so that superscripts
            // (e.g. ² reports fontSize=16 but real height≈4pt) don't extend a line's
            // MinY/MaxY far enough to cascade-merge the next visual line into the same group.
            var glyphHeight = (block.TopY > block.Y) ? (block.TopY - block.Y) : (block.FontSize * 0.7);
            var tolerance = Math.Max(minTolerance, Math.Min(block.FontSize * 0.5, glyphHeight));

            var group = groups.FirstOrDefault(g =>
                block.MidY >= g.MinY - tolerance && block.MidY <= g.MaxY + tolerance);

            if (group is null)
            {
                group = new LineGroup(block.MidY);
                groups.Add(group);
            }
            group.Add(block);
        }

        return groups;
    }

    // Subdivide um LineGroup em sub-grupos quando o range MidY excede 90% do fontSize
    // mediano — sinal de múltiplas linhas visuais fundidas.
    // Usa gap-based splitting (não cascata): ordena todos os MidY, coloca uma fronteira
    // em cada gap > minTolerance entre pares consecutivos. Isso evita que glifos de marca
    // posicionados ENTRE duas linhas do texto de campo façam a ponte entre elas.
    private static IEnumerable<LineGroup> SplitLineGroup(LineGroup group, double minTolerance)
    {
        if (group.Blocks.Count == 0) yield break;

        var sortedFontSizes = group.Blocks.Select(b => b.FontSize).OrderBy(f => f).ToList();
        double medianFontSize = sortedFontSizes[sortedFontSizes.Count / 2];
        double rangeY = group.MaxY - group.MinY;

        if (rangeY <= medianFontSize * 0.9)
        {
            yield return group;
            yield break;
        }

        // Collect split boundaries at every gap > minTolerance in the sorted MidY sequence.
        var sorted = group.Blocks.OrderBy(b => b.MidY).ToList();
        var boundaries = new List<double>();
        for (int i = 1; i < sorted.Count; i++)
        {
            double gap = sorted[i].MidY - sorted[i - 1].MidY;
            if (gap > minTolerance)
                boundaries.Add((sorted[i - 1].MidY + sorted[i].MidY) / 2.0);
        }

        if (boundaries.Count == 0)
        {
            yield return group;
            yield break;
        }

        // Assign each block to cluster[k] where k = number of boundaries strictly below MidY.
        // group.Blocks is in descending MidY order (from GroupIntoLines), so the first block
        // encountered for each cluster has the highest MidY in that cluster → correct BaseY.
        var subGroups = new LineGroup?[boundaries.Count + 1];
        foreach (var block in group.Blocks)
        {
            int idx = boundaries.Count(b => b < block.MidY);
            subGroups[idx] ??= new LineGroup(block.MidY);
            subGroups[idx]!.Add(block);
        }

        foreach (var sg in subGroups)
            if (sg is not null) yield return sg;
    }

    // Threshold adaptativo por linha: só ativa para fontes condensed onde os gaps
    // inter-word ficam abaixo do threshold padrão 0.21×fontSize.
    // Condições para considerar um split válido (intra-word vs inter-word):
    //   1. large_min > small_max × 1.5  — separação proporcional clara
    //   2. large_min > 0.12             — inter-word tem gap mínimo significativo (ratio)
    //   3. small_max < 0.15             — intra-word abaixo do limite seguro (ratio)
    // Se nenhum split satisfaz as três condições, retorna o fallback 0.21×fontSize.
    private static double ComputeLineThreshold(List<TextBlock> blocks)
    {
        if (blocks.Count < 4)
            return blocks[0].FontSize * 0.21;

        var ratios = new List<double>(blocks.Count - 1);
        for (int i = 1; i < blocks.Count; i++)
        {
            var g = (blocks[i].X - (blocks[i - 1].X + blocks[i - 1].Width)) / blocks[i - 1].FontSize;
            if (g > 0) ratios.Add(g);
        }
        if (ratios.Count < 3)
            return blocks[0].FontSize * 0.21;

        ratios.Sort();

        // Procurar o split com maior gap absoluto que satisfaça as três condições.
        double bestGap = 0;
        double bestMidpoint = -1;
        for (int i = 1; i < ratios.Count; i++)
        {
            double smallMax  = ratios[i - 1];
            double largeMin  = ratios[i];
            double absGap    = largeMin - smallMax;

            bool proportional   = largeMin > smallMax * 1.5;
            bool interWordLarge = largeMin > 0.12;
            bool intraWordSmall = smallMax < 0.15;

            if (proportional && interWordLarge && intraWordSmall && absGap > bestGap)
            {
                bestGap = absGap;
                bestMidpoint = (smallMax + largeMin) / 2.0;
            }
        }

        if (bestMidpoint > 0)
            return bestMidpoint * blocks[0].FontSize;

        return blocks[0].FontSize * 0.21;
    }

    public static string ConcatenateBlocks(List<TextBlock> blocks)
    {
        if (blocks.Count == 0)
            return string.Empty;

        var wordSpaceThreshold = ComputeLineThreshold(blocks);

        var sb = new StringBuilder();
        sb.Append(blocks[0].Text);

        for (int i = 1; i < blocks.Count; i++)
        {
            var prev = blocks[i - 1];
            var curr = blocks[i];
            var gap = curr.X - (prev.X + prev.Width);
            // Suprimir espaço quando dígito é seguido de outro dígito ou pontuação numérica (., /, -, :, ))
            // com gap marginal (até 0.27×). Ex: "19/05" ratio=0.23, "1)" ratio=0.19.
            // Dígito seguido de letra (ex: "14 de") não é suprimido — tem gap real e merece espaço.
            var nextIsNumeric = char.IsDigit(curr.Text[0]) || curr.Text[0] is '.' or '/' or '-' or ':' or ')';
            var digitMarginalGap = char.IsDigit(prev.Text[^1]) && nextIsNumeric && gap < prev.FontSize * 0.27;

            if (gap > wordSpaceThreshold && !digitMarginalGap
                && !prev.Text.EndsWith(' ') && !curr.Text.StartsWith(' '))
                sb.Append(' ');

            sb.Append(curr.Text);
        }

        return sb.ToString();
    }


}
