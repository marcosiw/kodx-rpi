using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Kodx.Rpi.Infrastructure.Tests;
using Microsoft.EntityFrameworkCore;

namespace Kodx.Rpi.Infrastructure.Tests.Persistence;

/// <summary>
/// Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente
/// (docker compose up -d). Sem Testcontainers por decisão do time.
/// </summary>
public sealed class KodxRpiDbContextTests : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
        ?? "Host=localhost;Port=5432;Database=kodx_rpi;Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await TestDatabaseMigrator.MigrateAsync(context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Salva_e_recupera_publicacao_com_payload_jsonb()
    {
        var edicao = new Random().Next(100000, 999999);
        int rpiEditionId;

        await using (var context = CreateContext())
        {
            var rpiEdition = new RpiEdition(edicao, RpiTipo.Patentes, DateTimeOffset.UtcNow);
            context.RpiEditions.Add(rpiEdition);
            await context.SaveChangesAsync();
            rpiEditionId = rpiEdition.Id;

            var payload = new PublicationPayload
            {
                Cabecalho1 = "(21) PI 1234567-8",
                Cabecalho2 = "(22) 01/01/2026",
                Conteudo = "Texto completo da publicação individualizada.",
                TodosNumeros = ["1234567-8"],
                IndexInicio = 100,
                IndexFim = 250,
                Pagina = 1
            };

            context.Publications.Add(new Publication(rpiEditionId, "1234567-8", payload));
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var publication = await context.Publications
                .SingleAsync(p => p.RpiEditionId == rpiEditionId && p.Numero == "1234567-8");

            Assert.Equal("(21) PI 1234567-8", publication.Payload.Cabecalho1);
            Assert.Equal("Texto completo da publicação individualizada.", publication.Payload.Conteudo);
            Assert.Equal(["1234567-8"], publication.Payload.TodosNumeros);
            Assert.Equal(1, publication.Payload.Pagina);
        }
    }

    [Fact]
    public async Task Registra_tentativa_de_processamento_com_falha()
    {
        var edicao = new Random().Next(100000, 999999);

        await using var context = CreateContext();
        var rpiEdition = new RpiEdition(edicao, RpiTipo.Marcas, DateTimeOffset.UtcNow);
        context.RpiEditions.Add(rpiEdition);
        await context.SaveChangesAsync();

        var startedAt = DateTimeOffset.UtcNow;
        var attempt = RpiProcessingAttempt.Failure(
            rpiEdition.Id,
            ProcessingStage.Download,
            "Timeout ao baixar do INPI",
            startedAt,
            startedAt.AddSeconds(30));

        context.RpiProcessingAttempts.Add(attempt);
        await context.SaveChangesAsync();

        var saved = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == rpiEdition.Id);
        Assert.Equal(ProcessingStatus.Failure, saved.Status);
        Assert.Equal(ProcessingStage.Download, saved.Stage);
        Assert.Equal("Timeout ao baixar do INPI", saved.ErrorMessage);
    }

    private static KodxRpiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KodxRpiDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new KodxRpiDbContext(options);
    }
}
