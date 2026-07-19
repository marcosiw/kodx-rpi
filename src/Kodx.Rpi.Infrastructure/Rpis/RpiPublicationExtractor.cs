using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

namespace Kodx.Rpi.Infrastructure.Rpis;

/// <summary>
/// Reproduz a cobertura real do sistema legado: só 4 dos 8 tipos de RPI têm regras de leitura
/// cadastradas (Marcas, Patentes, Desenhos Industriais, Programas de Computador) — os demais
/// nunca foram quebrados em publicações individuais em produção, e continuam sem suporte aqui.
/// </summary>
public sealed class RpiPublicationExtractor : IRpiPublicationExtractor
{
    private static readonly IReadOnlyDictionary<RpiTipo, Func<PublicationExtraction.PublicationTypeRules>> Rules = new Dictionary<RpiTipo, Func<PublicationExtraction.PublicationTypeRules>>
    {
        [RpiTipo.Marcas] = RpiMarcasRules.Create,
        [RpiTipo.Patentes] = RpiPatentesRules.Create,
        [RpiTipo.DesenhosIndustriais] = RpiDesenhosIndustriaisRules.Create,
        [RpiTipo.ProgramasComputador] = RpiProgramasComputadorRules.Create
    };

    public bool IsSupported(RpiTipo tipo) => Rules.ContainsKey(tipo);

    public IReadOnlyList<ExtractedPublication> Extract(RpiTipo tipo, string texto)
    {
        if (!Rules.TryGetValue(tipo, out var rulesFactory))
        {
            throw new NotSupportedException($"Não há regras de extração de publicações cadastradas para o tipo {tipo}.");
        }

        return RpiPublicationExtractionEngine.Extract(texto, rulesFactory());
    }
}
