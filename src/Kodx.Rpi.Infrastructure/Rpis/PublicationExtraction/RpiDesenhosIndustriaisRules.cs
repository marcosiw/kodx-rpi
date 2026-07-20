namespace Kodx.Rpi.Infrastructure.Rpis.PublicationExtraction;

/// <summary>Porta de RegrasDeNegocio/Leitura/RpiDesenhoIndustrial.cs do legado.</summary>
internal static class RpiDesenhosIndustriaisRules
{
    public static PublicationTypeRules Create() => new(
        SearchTerms:
        [
            new(@"[\r\n]BR[\d]{8,12}[\r\n]", IsRegex: true, PublicationFieldType.Publicacao, BreaksContinuity: true),
            new(@"[\r\n][(][\d]{2}[)] BR [\d]{2} [\d]{4} [\d-]{6,8}", IsRegex: true, PublicationFieldType.Publicacao, BreaksContinuity: true),
            new(@"[\r\n][(][\d]{2}[)] DI [\d-]{6,10}", IsRegex: true, PublicationFieldType.Publicacao, BreaksContinuity: true),
            new(@"Desenho Industrial - RPI [\d]{3,} de [\d]{1,2}[/][\d]{1,2}[/][\d]{2,4} [\d]{1,}[\r\n]", IsRegex: true, PublicationFieldType.Pagina, BreaksContinuity: false),
            new(@"Desenho Industrial – RPI [\d]{3,} de [\d]{1,2} de [A-zçã]{4,12} de [\d]{2,4} [\d]{1,}[/][\d]{1,}[\r\n]", IsRegex: true, PublicationFieldType.Pagina, BreaksContinuity: false),

            new("\r\nCódigo 109 - Recurso conhecido e provido. Reformada a Decisão recorrida\r\npara a concessão do registro.\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 111 - Recurso conhecido e negado provimento. Mantido o\r\nindeferimento do pedido\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 115 - Recurso conhecido e negado provimento. Mantida a Decisão\r\nrecorrida.\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 121 - Exigência\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 135 - Publicação Anulada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 137 - Petição Prejudicada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 200 - Nulidade conhecida e provida. Anulado o privilégio\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 201 - Nulidade conhecida e negado provimento. Mantida a concessão\r\ndo privilégio\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 205 - Intimação para manifestação por parte do titular e do requerente\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 211 - Sobrestado o Processo Administrativo\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 214 - Notificações Diversas\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 216 - Petição não Conhecida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 220 - Publicação Anulada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 30 - Exigência – Art. 103 da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 31 - Notificação de Depósito\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 33.1 - Pedido Inexistente\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 34 - Exigência - Art. 106 § 3º da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 35 - Arquivamento do Pedido – Art. 216 § 2º e Art. 106 § 3º da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 36 - Indeferimento - Art. 106 § 4º da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 37 - Recurso Contra o Indeferimento\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 38 - Outros Recursos\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 39 - Concessão do Registro\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 40 - Publicação do Parecer de Mérito\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 41 - Nulidade Administrativa\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 42 - Extinção - Art. 119 inciso I da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 43 - Extinção - Art. 119 inciso II da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 44 - Extinção - Art. 119 inciso III da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 46 - Prorrogação\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 46.2 - Exigência de complementação de qüinqüênio e/ou prorrogação\r\n– Art. 120 e 108 da LPI\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 47 - Petição Não Conhecida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 47.1 - Petição Prejudicada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 49 - Perda de Prioridade\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 50 - Alteração de Classificação\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 53 - Notificação de Decisão Judicial\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 53.1 - Pedido ou Registro Sub-Judice\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 54 - Devolução de Prazo Concedida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 55 - Exigências Diversas\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 56 - Transferência Deferida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 58 - Transferência em Exigência\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 59 - Alteração de Nome Deferida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 61 - Alteração de Nome em Exigência\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 62 - Alteração de Sede Deferida\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 64 - Alteração de Sede em Exigência\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 70 - Publicação Anulada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 71 - Despacho Anulado\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 72 - Decisão Anulada\r\n", false, PublicationFieldType.Cabecalho1, true),
            new("\r\nCódigo 73 - Retificação\r\n", false, PublicationFieldType.Cabecalho1, true)
        ],
        SyntheticField: PublicationFieldType.Cabecalho2,
        SyntheticReferenceField: PublicationFieldType.Cabecalho1,
        ProcessNumberPatterns:
        [
            @"BR [\d]{2} [\d]{4} [\d-]{6,8}",
            @"DI [\d-]{6,10}"
        ]);
}
