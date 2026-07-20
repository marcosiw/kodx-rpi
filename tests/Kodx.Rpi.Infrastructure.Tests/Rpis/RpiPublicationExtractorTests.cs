using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Rpis;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>
/// Testes de unidade do parser de publicações, com trechos de texto montados a partir dos
/// termos literais/regex reais do legado (não são PDFs reais, mas exercitam exatamente os
/// mesmos padrões usados em produção) — a validação final de ponta a ponta acontece com dados
/// reais na outra máquina, comparando com o que o legado já processou.
/// </summary>
public sealed class RpiPublicationExtractorTests
{
    private readonly RpiPublicationExtractor extractor = new();

    [Theory]
    [InlineData(RpiTipo.Marcas)]
    [InlineData(RpiTipo.Patentes)]
    [InlineData(RpiTipo.DesenhosIndustriais)]
    [InlineData(RpiTipo.ProgramasComputador)]
    public void Tipos_com_regras_cadastradas_sao_suportados(RpiTipo tipo)
    {
        Assert.True(extractor.IsSupported(tipo));
    }

    [Fact]
    public void Tipo_sem_regras_cadastradas_nao_e_suportado_e_lanca_ao_extrair()
    {
        Assert.False(extractor.IsSupported(RpiTipo.Comunicados));
        Assert.Throws<NotSupportedException>(() => extractor.Extract(RpiTipo.Comunicados, "qualquer texto"));
    }

    [Fact]
    public void Marcas_quebra_publicacoes_herda_cabecalhos_e_limpa_pagina_do_cabecalho3_sintetico()
    {
        var texto =
            "\r\nConcessões de registros de marca\r\n" +
            "\r\nRegistro de marca concedido\r\n" +
            "MARCAS - RPI 2891 de 15/06/2026 1\r\n" +
            "\r\n900123456\r\nTitular Empresa A Ltda processo 900123456 concedido.\r\n" +
            "\r\nRegistros de marca extintos\r\n" +
            "\r\n900987654\r\nTitular Empresa B Ltda processo 900987654 extinto.\r\n";

        var publicacoes = extractor.Extract(RpiTipo.Marcas, texto);

        Assert.Equal(2, publicacoes.Count);

        var primeira = publicacoes[0];
        Assert.Equal("Concessões de registros de marca", primeira.Payload.Cabecalho1);
        Assert.Equal("Registro de marca concedido", primeira.Payload.Cabecalho2);
        // "\r" solto (não ""): a regex de fronteira de publicação só consome o \n do \r\n
        // anterior, e o LimparParagrafos do legado (portado fielmente) não limpa esse \r
        // residual — comportamento real de produção, não um bug do port.
        Assert.Equal("\r", primeira.Payload.Cabecalho3);
        Assert.Contains("Titular Empresa A", primeira.Payload.Conteudo);
        Assert.Equal("900123456", primeira.Numero);
        // O número aparece duas vezes no conteúdo (na fronteira e na frase) — TodosNumeros
        // acumula todas as ocorrências, sem deduplicar, igual ao legado.
        Assert.Equal(["900123456", "900123456"], primeira.Payload.TodosNumeros);
        Assert.Equal(1, primeira.Payload.Pagina);

        var segunda = publicacoes[1];
        // Termina com "\r\n" residual pelo mesmo motivo do Cabecalho3 acima: este cabeçalho é
        // seguido diretamente pela fronteira regex da publicação 2, que só consome o \n.
        Assert.StartsWith("Registros de marca extintos", segunda.Payload.Cabecalho1);
        // Cabecalho2 é herdado do último marcador visto antes desta publicação — não há um novo.
        Assert.Equal("Registro de marca concedido", segunda.Payload.Cabecalho2);
        Assert.Equal("900987654", segunda.Numero);
    }

    [Fact]
    public void Patentes_numero_fica_com_o_ultimo_padrao_que_casou_mas_todosNumeros_acumula_todos()
    {
        var texto =
            "\r\nCódigo Depósito\r\n" +
            "\r\nCódigo 9.1 - Deferimento\r\n" +
            "PATENTES - RPI 2891 de 15/06/2026 5\r\n" +
            "\r\n(21) BR 10 2020 001234-5\r\nTeste. Também referencia (22) PI 1234567-8 em petição.\r\n";

        var publicacoes = extractor.Extract(RpiTipo.Patentes, texto);

        var publicacao = Assert.Single(publicacoes);
        Assert.Equal("Código Depósito", publicacao.Payload.Cabecalho1);
        Assert.Equal("Código 9.1 - Deferimento", publicacao.Payload.Cabecalho2);
        Assert.Equal(5, publicacao.Payload.Pagina);

        // BR é testado antes de PI na lista de padrões; como os dois casam, PI (o último a
        // casar) fica com Numero — reproduz fielmente a peculiaridade do legado.
        Assert.Equal("PI 1234567-8", publicacao.Numero);
        Assert.Equal(["BR 10 2020 001234-5", "PI 1234567-8"], publicacao.Payload.TodosNumeros);
    }

    [Fact]
    public void DesenhosIndustriais_gera_cabecalho2_sintetico_a_partir_do_cabecalho1()
    {
        var texto =
            "\r\nCódigo 39 - Concessão do Registro\r\n" +
            "Desenho Industrial - RPI 2891 de 15/06/2026 7\r\n" +
            "\r\nBR302020001234\r\nRegistro concedido, processo DI 123456-7 depositado anteriormente.\r\n";

        var publicacoes = extractor.Extract(RpiTipo.DesenhosIndustriais, texto);

        var publicacao = Assert.Single(publicacoes);
        Assert.Equal("Código 39 - Concessão do Registro", publicacao.Payload.Cabecalho1);
        // "\r" solto pelo mesmo motivo do teste de Marcas (ver comentário lá).
        Assert.Equal("\r", publicacao.Payload.Cabecalho2);
        Assert.Equal("", publicacao.Payload.Cabecalho3);
        Assert.Equal("DI 123456-7", publicacao.Numero);
        Assert.Equal(7, publicacao.Payload.Pagina);
    }

    [Fact]
    public void ProgramasComputador_le_pagina_no_formato_pagina_barra_total()
    {
        var texto =
            "\r\nCódigo 120 - Concessão do Registro\r\n" +
            "Programas de Computador – RPI 2891 de 15 de junho de 2026 3/50\r\n" +
            "\r\nProcesso: BR 10 2020 001234-5\r\nRegistro concedido para o programa X.\r\n";

        var publicacoes = extractor.Extract(RpiTipo.ProgramasComputador, texto);

        var publicacao = Assert.Single(publicacoes);
        Assert.Equal("Código 120 - Concessão do Registro", publicacao.Payload.Cabecalho1);
        Assert.Equal("BR 10 2020 001234-5", publicacao.Numero);
        Assert.Equal(3, publicacao.Payload.Pagina);
    }
}
