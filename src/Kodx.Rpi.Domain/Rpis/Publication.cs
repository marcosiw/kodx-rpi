namespace Kodx.Rpi.Domain.Rpis;

public sealed class Publication
{
    public int Id { get; private set; }
    public int RpiEditionId { get; private set; }
    public string Numero { get; private set; } = string.Empty;
    public PublicationPayload Payload { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Todos os números de processo mencionados nesta publicação — número primário
    /// (<see cref="Numero"/>) mais os secundários de <see cref="PublicationPayload.TodosNumeros"/>,
    /// deduplicados. Usado pelo repositório pra popular <see cref="PublicationNumero"/>, a
    /// tabela relacional indexada de busca (diferente de TodosNumeros, que fica dentro do
    /// jsonb do Payload e não é indexável — TodosNumeros não deduplica, fiel ao legado; aqui
    /// deduplicamos porque isto é só um índice de busca, não um espelho do dado bruto).
    /// </summary>
    public IReadOnlyCollection<string> Numeros { get; } = [];

    private Publication()
    {
    }

    public Publication(int rpiEditionId, string numero, PublicationPayload payload)
    {
        RpiEditionId = rpiEditionId;
        Numero = numero;
        Payload = payload;
        CreatedAt = DateTimeOffset.UtcNow;
        Numeros = payload.TodosNumeros.Append(numero).Distinct().ToArray();
    }
}
