namespace Kodx.Rpi.Infrastructure.Pdf;

public class LineGroup(double baseMidY)
{
    public double BaseY    { get; } = baseMidY;
    public double MinY     { get; private set; } = baseMidY;
    public double MaxY     { get; private set; } = baseMidY;
    public List<TextBlock> Blocks { get; } = [];

    public void Add(TextBlock block)
    {
        Blocks.Add(block);
        if (block.MidY < MinY) MinY = block.MidY;
        if (block.MidY > MaxY) MaxY = block.MidY;
    }
}
