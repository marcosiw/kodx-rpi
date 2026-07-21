using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Kodx.Rpi.Api.Grpc;
using Kodx.Rpi.Api.Tests;
using Microsoft.EntityFrameworkCore;
using RpiDomain = Kodx.Rpi.Domain.Rpis;

namespace Kodx.Rpi.Api.Tests.Grpc;

/// <summary>
/// Testes de integração: sobem a Api real via TestServer (WebApplicationFactory) e chamam o
/// serviço gRPC por um GrpcChannel de verdade — exigem o Postgres do docker-compose.yml
/// rodando localmente. Fila de background e Blob Storage são fakes (ver RpiApiFactory).
/// </summary>
public sealed class RpiGrpcServiceTests(RpiApiFactory factory) : IClassFixture<RpiApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var context = factory.CreateDbContext();
        await TestDatabaseMigrator.MigrateAsync(context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private RpiService.RpiServiceClient CreateClient()
    {
        var httpClient = factory.CreateClient();
        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions { HttpClient = httpClient });
        return new RpiService.RpiServiceClient(channel);
    }

    private static Metadata ApiKeyHeader() => new() { { "x-api-key", RpiApiFactory.ApiKey } };

    [Fact]
    public async Task TriggerDownload_enfileira_o_job_e_responde_na_hora()
    {
        // factory.Queue é compartilhada entre os testes da classe (IClassFixture) — compara
        // a contagem antes/depois em vez de assumir fila vazia.
        var enqueuedBefore = factory.Queue.Enqueued.Count;
        var client = CreateClient();
        var edicao = Random.Shared.Next(100000, 999999);

        var reply = await client.TriggerDownloadAsync(
            new TriggerDownloadRequest { Tipo = RpiTipo.Patentes, Edicao = edicao },
            ApiKeyHeader());

        Assert.Equal(RpiTipo.Patentes, reply.Tipo);
        Assert.Equal(edicao, reply.Edicao);
        Assert.Equal(enqueuedBefore + 1, factory.Queue.Enqueued.Count);
    }

    [Fact]
    public async Task TriggerDownload_sem_edicao_nao_preenche_o_campo_na_resposta()
    {
        var client = CreateClient();

        var reply = await client.TriggerDownloadAsync(
            new TriggerDownloadRequest { Tipo = RpiTipo.Marcas },
            ApiKeyHeader());

        Assert.False(reply.HasEdicao);
    }

    [Fact]
    public async Task Chamada_sem_api_key_e_rejeitada()
    {
        var client = CreateClient();

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            client.TriggerDownloadAsync(new TriggerDownloadRequest { Tipo = RpiTipo.Patentes }).ResponseAsync);

        // ApiKeyMiddleware responde 401 HTTP puro (fora do protocolo gRPC) — Grpc.Net.Client
        // mapeia isso pra Unauthenticated no cliente (confirmado empiricamente contra o
        // TestServer real, não assumido).
        Assert.Equal(StatusCode.Unauthenticated, ex.StatusCode);
    }

    [Fact]
    public async Task GetRpiHistory_edicao_inexistente_retorna_not_found()
    {
        var client = CreateClient();
        var edicao = Random.Shared.Next(100000, 999999);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            client.GetRpiHistoryAsync(new GetRpiHistoryRequest { Tipo = RpiTipo.Marcas, Edicao = edicao }, ApiKeyHeader()).ResponseAsync);

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task GetRpiHistory_retorna_dados_da_edicao_e_historico_de_tentativas()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var editionId = await SeedEditionAsync(edicao, RpiDomain.RpiTipo.Marcas);
        await SeedAttemptAsync(editionId, RpiDomain.ProcessingStage.Download);

        var client = CreateClient();
        var reply = await client.GetRpiHistoryAsync(new GetRpiHistoryRequest { Tipo = RpiTipo.Marcas, Edicao = edicao }, ApiKeyHeader());

        Assert.Equal(edicao, reply.Edicao);
        Assert.Equal(RpiTipo.Marcas, reply.Tipo);
        var attempt = Assert.Single(reply.Attempts);
        Assert.Equal(ProcessingStage.Download, attempt.Stage);
        Assert.Equal(ProcessingStatus.Success, attempt.Status);
        Assert.Equal(string.Empty, attempt.ErrorMessage);
    }

    [Fact]
    public async Task DownloadPdf_edicao_inexistente_retorna_not_found()
    {
        var client = CreateClient();
        var edicao = Random.Shared.Next(100000, 999999);
        var call = client.DownloadPdf(new DownloadPdfRequest { Tipo = RpiTipo.Patentes, Edicao = edicao }, ApiKeyHeader());

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var _ in call.ResponseStream.ReadAllAsync())
            {
            }
        });

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DownloadPdf_transmite_o_conteudo_em_mais_de_um_chunk()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        await SeedEditionAsync(edicao, RpiDomain.RpiTipo.Patentes);

        var conteudo = Encoding.UTF8.GetBytes(new string('a', 200_000)); // > 64KB (tamanho do chunk)
        factory.BlobStorage.PdfContent = conteudo;

        var client = CreateClient();
        var call = client.DownloadPdf(new DownloadPdfRequest { Tipo = RpiTipo.Patentes, Edicao = edicao }, ApiKeyHeader());

        using var received = new MemoryStream();
        var chunkCount = 0;
        await foreach (var chunk in call.ResponseStream.ReadAllAsync())
        {
            chunkCount++;
            chunk.Data.WriteTo(received);
        }

        Assert.True(chunkCount > 1, $"Esperava mais de 1 chunk, recebeu {chunkCount}.");
        Assert.Equal(conteudo, received.ToArray());
    }

    [Fact]
    public async Task SearchRpiPublications_retorna_so_as_publicacoes_pedidas()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var editionId = await SeedEditionAsync(edicao, RpiDomain.RpiTipo.Marcas);
        await SeedPublicationAsync(editionId, "900111111");
        await SeedPublicationAsync(editionId, "900222222");
        await SeedPublicationAsync(editionId, "900333333"); // não pedido, não deve voltar

        var client = CreateClient();
        var request = new SearchRpiPublicationsRequest { Tipo = RpiTipo.Marcas, Edicao = edicao };
        request.Numeros.AddRange(["900111111", "900222222"]);
        var call = client.SearchRpiPublications(request, ApiKeyHeader());

        var numeros = new List<string>();
        await foreach (var reply in call.ResponseStream.ReadAllAsync())
        {
            numeros.Add(reply.Numero);
            Assert.Equal(RpiTipo.Marcas, reply.Tipo);
            Assert.Equal(edicao, reply.Edicao);
        }

        Assert.Equal(["900111111", "900222222"], numeros);
    }

    [Fact]
    public async Task SearchRpiPublications_sem_tipo_e_edicao_busca_em_todo_o_historico()
    {
        var numero = $"9{Random.Shared.Next(100000000, 999999999)}";
        var edicao1 = Random.Shared.Next(100000, 199999);
        var edicao2 = Random.Shared.Next(200000, 299999);
        var editionId1 = await SeedEditionAsync(edicao1, RpiDomain.RpiTipo.Marcas);
        var editionId2 = await SeedEditionAsync(edicao2, RpiDomain.RpiTipo.Patentes);
        await SeedPublicationAsync(editionId1, numero);
        await SeedPublicationAsync(editionId2, numero);

        var client = CreateClient();
        var request = new SearchRpiPublicationsRequest();
        request.Numeros.Add(numero);
        var call = client.SearchRpiPublications(request, ApiKeyHeader());

        var encontrados = new List<(RpiTipo Tipo, int Edicao)>();
        await foreach (var reply in call.ResponseStream.ReadAllAsync())
        {
            encontrados.Add((reply.Tipo, reply.Edicao));
        }

        Assert.Equal([(RpiTipo.Marcas, edicao1), (RpiTipo.Patentes, edicao2)], encontrados);
    }

    [Fact]
    public async Task SearchRpiPublications_sem_numeros_retorna_invalid_argument()
    {
        var client = CreateClient();
        var call = client.SearchRpiPublications(new SearchRpiPublicationsRequest(), ApiKeyHeader());

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var _ in call.ResponseStream.ReadAllAsync())
            {
            }
        });

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task SearchRpiPublications_so_tipo_sem_edicao_retorna_invalid_argument()
    {
        var client = CreateClient();
        var request = new SearchRpiPublicationsRequest { Tipo = RpiTipo.Marcas };
        request.Numeros.Add("900111111");
        var call = client.SearchRpiPublications(request, ApiKeyHeader());

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var _ in call.ResponseStream.ReadAllAsync())
            {
            }
        });

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task SearchRpiPublications_edicao_inexistente_retorna_not_found()
    {
        var client = CreateClient();
        var edicao = Random.Shared.Next(100000, 999999);
        var request = new SearchRpiPublicationsRequest { Tipo = RpiTipo.Marcas, Edicao = edicao };
        request.Numeros.Add("900111111");
        var call = client.SearchRpiPublications(request, ApiKeyHeader());

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await foreach (var _ in call.ResponseStream.ReadAllAsync())
            {
            }
        });

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    private async Task<int> SeedEditionAsync(int edicao, RpiDomain.RpiTipo tipo)
    {
        await using var context = factory.CreateDbContext();
        var edition = new RpiDomain.RpiEdition(edicao, tipo, DateTimeOffset.UtcNow);
        context.RpiEditions.Add(edition);
        await context.SaveChangesAsync();
        return edition.Id;
    }

    private async Task SeedAttemptAsync(int editionId, RpiDomain.ProcessingStage stage)
    {
        await using var context = factory.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        context.RpiProcessingAttempts.Add(RpiDomain.RpiProcessingAttempt.Success(editionId, stage, now, now));
        await context.SaveChangesAsync();
    }

    private async Task SeedPublicationAsync(int editionId, string numero)
    {
        // Mesmo padrão de duas fases do PublicationRepository.ReplaceForEditionAsync: precisa
        // do Id gerado da Publication antes de gravar a PublicationNumero (FK). Inserir só a
        // Publication (sem passar pelo repositório) não popula a tabela de busca sozinho.
        await using var context = factory.CreateDbContext();
        var publication = new RpiDomain.Publication(editionId, numero, new RpiDomain.PublicationPayload());
        context.Publications.Add(publication);
        await context.SaveChangesAsync();

        context.PublicationNumeros.Add(new RpiDomain.PublicationNumero(publication.Id, numero));
        await context.SaveChangesAsync();
    }
}
