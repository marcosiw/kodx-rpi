using Kodx.Rpi.Infrastructure.Rpis;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

public sealed class InpiRpiCalendarTests
{
    // Recorte real da tabela de https://revistas.inpi.gov.br/rpi/ (mais recente primeiro).
    private const string SampleHtml = """
        <table>
        <tr class="warning">
            <td>2897</td>
            <td>
                2026-07-14
            </td>
        </tr>
        <tr>
            <td>2896</td>
            <td>
                2026-07-07
            </td>
        </tr>
        <tr>
            <td>2891</td>
            <td>
                2026-06-02
            </td>
        </tr>
        </table>
        """;

    [Fact]
    public async Task GetMostRecentEditionAsync_retorna_a_primeira_linha_da_tabela()
    {
        var calendar = CreateCalendar(SampleHtml);

        var entry = await calendar.GetMostRecentEditionAsync(CancellationToken.None);

        Assert.NotNull(entry);
        Assert.Equal(2897, entry.Edicao);
        Assert.Equal(new DateOnly(2026, 7, 14), entry.DataPublicacao);
    }

    [Fact]
    public async Task GetPublicationDateAsync_encontra_edicao_especifica_na_tabela()
    {
        var calendar = CreateCalendar(SampleHtml);

        var date = await calendar.GetPublicationDateAsync(2891, CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 6, 2), date);
    }

    [Fact]
    public async Task GetPublicationDateAsync_retorna_null_para_edicao_fora_da_tabela()
    {
        var calendar = CreateCalendar(SampleHtml);

        var date = await calendar.GetPublicationDateAsync(9999, CancellationToken.None);

        Assert.Null(date);
    }

    [Fact]
    public async Task GetMostRecentEditionAsync_retorna_null_quando_pagina_muda_de_formato()
    {
        var calendar = CreateCalendar("<html><body>página diferente, sem tabela reconhecível</body></html>");

        var entry = await calendar.GetMostRecentEditionAsync(CancellationToken.None);

        Assert.Null(entry);
    }

    [Fact]
    public async Task GetMostRecentEditionAsync_retorna_null_quando_http_falha()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("simulado"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://revistas.inpi.gov.br/rpi/") };
        var calendar = new InpiRpiCalendar(httpClient, NullLogger<InpiRpiCalendar>.Instance);

        var entry = await calendar.GetMostRecentEditionAsync(CancellationToken.None);

        Assert.Null(entry);
    }

    private static InpiRpiCalendar CreateCalendar(string html)
    {
        var handler = new StubHttpMessageHandler((_, _) => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://revistas.inpi.gov.br/rpi/") };
        return new InpiRpiCalendar(httpClient, NullLogger<InpiRpiCalendar>.Instance);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request, cancellationToken));
    }
}
