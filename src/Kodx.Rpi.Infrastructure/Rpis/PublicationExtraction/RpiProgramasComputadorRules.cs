namespace Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

/// <summary>Porta de RegrasDeNegocio/Leitura/RpiProgramasdeComputador.cs do legado.</summary>
internal static class RpiProgramasComputadorRules
{
    public static PublicationTypeRules Create() => new(
        SearchTerms:
        [
            new(@"[\r\n]Processo: BR [\d]{2} [\d]{4} [\d-]{6,8}", IsRegex: true, PublicationFieldType.Publicacao, BreaksContinuity: true),
            new(@"[\r\n]Processo: [\d-]{5,10}", IsRegex: true, PublicationFieldType.Publicacao, BreaksContinuity: true),
            new(@"Programas de Computador – RPI [\d]{3,} de [\d]{1,2} de [A-zçã]{4,12} de [\d]{2,4} [\d]{1,}[/][\d]{1,}[\r\n]", IsRegex: true, PublicationFieldType.Pagina, BreaksContinuity: false),

            new("\r\nCódigo 080 - Publicação de pedido de Registro de Programa de Computador\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 082 - Pedido em exigência devido a irregularidades\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 090 - Deferimento de pedido de registro de programa de computador\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 091 - Alteração de Nome Deferida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 097 - Alteração de Endereço Deferida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 100 - Transferência de Titularidade Deferida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 104 - Petição não conhecida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 106 - Renúncia ao registro de programa de computador homologada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 110 - Publicação Anulada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 111 - Despacho Anulado\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 113 - Retificação\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 120 - Concessão do Registro\r\n", false, PublicationFieldType.Cabecalho1, true)
        ],
        SyntheticField: PublicationFieldType.Cabecalho2,
        SyntheticReferenceField: PublicationFieldType.Cabecalho1,
        ProcessNumberPatterns: [@"BR [\d]{2} [\d]{4} [\d-]{6,8}"]);
}
