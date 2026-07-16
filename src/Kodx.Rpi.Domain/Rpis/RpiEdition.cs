namespace Kodx.Rpi.Domain.Rpis;

public sealed class RpiEdition
{
    public int Id { get; private set; }
    public int Edicao { get; private set; }
    public RpiTipo Tipo { get; private set; }
    public DateTimeOffset DataPublicacao { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RpiEdition()
    {
    }

    public RpiEdition(int edicao, RpiTipo tipo, DateTimeOffset dataPublicacao)
    {
        Edicao = edicao;
        Tipo = tipo;
        DataPublicacao = dataPublicacao;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
