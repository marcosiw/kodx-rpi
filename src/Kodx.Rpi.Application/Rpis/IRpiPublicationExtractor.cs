using Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Application.Rpis;

/// <summary>
/// Quebra o texto completo de uma RPI em publicações individuais, reproduzindo as regras de
/// leitura já usadas em produção pelo sistema legado. Só alguns tipos de RPI têm regras
/// cadastradas (ver <see cref="IsSupported"/>) — os demais nunca foram quebrados em publicações
/// individuais em produção.
/// </summary>
public interface IRpiPublicationExtractor
{
    bool IsSupported(RpiTipo tipo);

    IReadOnlyList<ExtractedPublication> Extract(RpiTipo tipo, string texto);
}
