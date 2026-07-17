namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class RpiStorageOptions
{
    public const string SectionName = "RpiStorage";

    /// <summary>Diretório local de trabalho onde PDFs baixados ficam até serem convertidos/enviados ao Blob Storage (fases futuras).</summary>
    public string LocalWorkingDirectory { get; set; } = "./data/rpi";
}
