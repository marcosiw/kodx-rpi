namespace Kodx.Rpi.Domain.Rpis;

/// <summary>
/// Convenção de nomes de arquivo usada pelo INPI e reaproveitada pelo sistema legado
/// (inclui o typo "circuto" em TopografiaCircuitos — precisa bater com a URL real).
/// </summary>
public static class RpiFileNaming
{
    private static readonly IReadOnlyDictionary<RpiTipo, string> Prefixes = new Dictionary<RpiTipo, string>
    {
        [RpiTipo.Comunicados] = "Comunicados",
        [RpiTipo.ContratosTecnologia] = "Contratos_de_Tecnologia",
        [RpiTipo.DesenhosIndustriais] = "Desenhos_Industriais",
        [RpiTipo.IndicacoesGeograficas] = "Indicacoes_Geograficas",
        [RpiTipo.Marcas] = "Marcas",
        [RpiTipo.Patentes] = "Patentes",
        [RpiTipo.ProgramasComputador] = "Programas_de_Computador",
        [RpiTipo.TopografiaCircuitos] = "Topografia_de_circuto_Integrado"
    };

    public static string PdfFileName(RpiTipo tipo, int edicao) => $"{Prefixes[tipo]}{edicao}.pdf";

    public static string TxtFileName(RpiTipo tipo, int edicao) => $"{Prefixes[tipo]}{edicao}.txt";
}
