namespace Kodx.Rpi.Infrastructure.Rpis;

public sealed class InpiOptions
{
    public const string SectionName = "Inpi";

    /// <summary>URL do site do INPI de onde as RPIs em pdf são baixadas.</summary>
    public string BaseUrl { get; set; } = "http://revistas.inpi.gov.br/pdf/";

    /// <summary>Página com o calendário oficial de edições (número + data de publicação real, já ajustada por feriado).</summary>
    public string CalendarUrl { get; set; } = "https://revistas.inpi.gov.br/rpi/";

    /// <summary>Timeout do download em segundos. Alto porque roda em background, não amarrado ao timeout de request da API.</summary>
    public int HttpTimeoutSeconds { get; set; } = 300;

    /// <summary>O INPI bloqueia (403) requisições sem User-Agent de navegador.</summary>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36";
}
