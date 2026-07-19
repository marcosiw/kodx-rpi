namespace Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

/// <summary>
/// Regras de leitura de um tipo de RPI (porta de RegrasDeNegocio/Leitura/Rpi*.cs do legado):
/// os termos de busca que marcam publicação/cabeçalhos/página, o campo sintético derivado de
/// outro campo (nenhum dos 4 tipos suportados tem cabecalho3 como termo literal — ele sempre
/// vem "de graça" logo após o fim do cabecalho2 ou, pra Desenhos/Programas, o cabecalho2 vem
/// logo após o fim do cabecalho1) e os padrões de número de processo aplicados ao conteúdo de
/// cada publicação.
/// </summary>
internal sealed record PublicationTypeRules(
    IReadOnlyList<PublicationSearchTerm> SearchTerms,
    PublicationFieldType SyntheticField,
    PublicationFieldType SyntheticReferenceField,
    IReadOnlyList<string> ProcessNumberPatterns);
