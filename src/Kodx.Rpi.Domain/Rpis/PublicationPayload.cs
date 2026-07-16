namespace Kodx.Rpi.Domain.Rpis;

/// <summary>Conteúdo bruto da publicação individual, persistido em jsonb. Espelha os campos de cabeçalho/conteúdo do sistema legado.</summary>
public sealed class PublicationPayload
{
    public string? Cabecalho1 { get; set; }
    public string? Cabecalho2 { get; set; }
    public string? Cabecalho3 { get; set; }
    public string? Conteudo { get; set; }
    public IReadOnlyCollection<string> TodosNumeros { get; set; } = [];
    public int IndexInicio { get; set; }
    public int IndexFim { get; set; }
    public string? Rodape { get; set; }
    public int Pagina { get; set; }
    public string? Orgao { get; set; }
}
