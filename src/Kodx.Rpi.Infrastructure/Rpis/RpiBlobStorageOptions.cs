namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class RpiBlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = "";

    /// <summary>Mesmo container usado pelo legado (Kodx.Producao) — mantém compatibilidade com o backup já existente.</summary>
    public string ContainerName { get; set; } = "rpi";
}
