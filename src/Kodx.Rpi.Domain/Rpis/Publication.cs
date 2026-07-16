namespace Kodx.Rpi.Domain.Rpis;

public sealed class Publication
{
    public int Id { get; private set; }
    public int RpiEditionId { get; private set; }
    public string Numero { get; private set; } = string.Empty;
    public PublicationPayload Payload { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    private Publication()
    {
    }

    public Publication(int rpiEditionId, string numero, PublicationPayload payload)
    {
        RpiEditionId = rpiEditionId;
        Numero = numero;
        Payload = payload;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
