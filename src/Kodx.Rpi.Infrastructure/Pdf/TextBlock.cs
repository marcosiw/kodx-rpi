namespace Kodx.Rpi.Infrastructure.Pdf;

public record TextBlock(string Text, double X, double Y, double Width, double FontSize)
{
    // TopY of the glyph bounding box; 0 means unknown (tests may omit it).
    public double TopY { get; init; }

    // Used for line grouping: the center of the glyph rectangle.
    // More stable than BottomLeft.Y for superscripts and floating punctuation
    // (e.g. curly quotes whose bottom sits between two visual lines).
    // Falls back to an estimate when TopY is not provided.
    public double MidY => TopY > 0
        ? (Y + TopY) / 2.0
        : Y + FontSize * 0.35;
}
