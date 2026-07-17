namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class RpiStorageOptions
{
    public const string SectionName = "RpiStorage";

    /// <summary>
    /// Diretório local de trabalho onde PDFs baixados (e, em fases futuras, os TXTs convertidos)
    /// ficam até serem enviados ao Blob Storage. Relativo ao content root da Api
    /// (src/Kodx.Rpi.Api) — "../../data/rpi" cai na raiz do repositório.
    /// </summary>
    public string LocalWorkingDirectory { get; set; } = "../../data/rpi";
}
