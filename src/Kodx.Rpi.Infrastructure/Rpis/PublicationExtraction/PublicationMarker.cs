namespace Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

/// <summary>Marcação de posição no texto da RPI (porta de RepositorioIndex do legado).</summary>
internal sealed class PublicationMarker
{
    public required int Start { get; init; }
    public int End { get; set; }
    public required PublicationFieldType Field { get; init; }
}
