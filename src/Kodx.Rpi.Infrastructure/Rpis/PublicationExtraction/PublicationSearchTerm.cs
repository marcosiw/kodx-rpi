namespace Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

/// <summary>
/// Um termo de busca usado pra marcar limites de campo no texto da RPI (porta de
/// RepositorioPesquisa do legado). <paramref name="BreaksContinuity"/> distingue marcadores que
/// abrem um novo bloco de conteúdo (publicação, cabeçalhos) dos que só marcam posição sem
/// interromper o bloco corrente (rodapé/cabeçalho de página, usado só pra descobrir a página).
/// </summary>
internal sealed record PublicationSearchTerm(string Pattern, bool IsRegex, PublicationFieldType Field, bool BreaksContinuity);
