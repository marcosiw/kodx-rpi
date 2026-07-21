namespace Kodx.Rpi.Domain.Rpis;

/// <summary>
/// Índice relacional (indexado) de cada número de processo mencionado numa publicação —
/// primário e secundários. Existe separado do jsonb de <see cref="PublicationPayload.TodosNumeros"/>
/// porque esse não é buscável de forma eficiente; esta tabela é só pra suportar busca por
/// número.
/// </summary>
public sealed class PublicationNumero
{
    public int Id { get; private set; }
    public int PublicationId { get; private set; }
    public string Numero { get; private set; } = string.Empty;

    private PublicationNumero()
    {
    }

    public PublicationNumero(int publicationId, string numero)
    {
        PublicationId = publicationId;
        Numero = numero;
    }
}
