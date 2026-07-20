using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente. Usa um extrator fake — a lógica real do parser é coberta por RpiPublicationExtractorTests.cs.</summary>
public sealed class ExtractRpiPublicationsUseCaseTests : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
        ?? "Host=localhost;Port=5432;Database=kodx_rpi;Username=postgres;Password=postgres";

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Tipo_nao_suportado_nao_registra_tentativa_nem_extrai()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var extractor = new FakeExtractor(supported: false);

        var editionId = await SeedEdition(edicao, RpiTipo.Comunicados);
        await Execute(RpiTipo.Comunicados, edicao, extractor);

        await using var context = CreateContext();
        var attempts = await context.RpiProcessingAttempts.Where(a => a.RpiEditionId == editionId).ToListAsync();
        Assert.Empty(attempts);
    }

    [Fact]
    public async Task Extracao_com_sucesso_grava_tentativa_e_substitui_publicacoes_existentes()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var editionId = await SeedEdition(edicao, RpiTipo.Patentes);

        await using (var context = CreateContext())
        {
            context.Publications.Add(new Publication(editionId, "velho", new PublicationPayload { Conteudo = "obsoleta" }));
            await context.SaveChangesAsync();
        }

        var extracted = new[]
        {
            new ExtractedPublication("PI 1111111-1", new PublicationPayload { Conteudo = "conteúdo 1" }),
            new ExtractedPublication("PI 2222222-2", new PublicationPayload { Conteudo = "conteúdo 2" })
        };
        var extractor = new FakeExtractor(supported: true, extracted);

        await Execute(RpiTipo.Patentes, edicao, extractor);

        await using var verifyContext = CreateContext();
        var attempt = await verifyContext.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);
        Assert.Equal(ProcessingStatus.Success, attempt.Status);
        Assert.Equal(ProcessingStage.ExtractPublications, attempt.Stage);

        var publicacoes = await verifyContext.Publications.Where(p => p.RpiEditionId == editionId).ToListAsync();
        Assert.Equal(2, publicacoes.Count);
        Assert.DoesNotContain(publicacoes, p => p.Numero == "velho");
        Assert.Contains(publicacoes, p => p.Numero == "PI 1111111-1");
        Assert.Contains(publicacoes, p => p.Numero == "PI 2222222-2");
    }

    [Fact]
    public async Task Falha_na_leitura_do_txt_grava_tentativa_de_falha()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var editionId = await SeedEdition(edicao, RpiTipo.Marcas);
        var extractor = new FakeExtractor(supported: true, shouldThrow: true);

        await Execute(RpiTipo.Marcas, edicao, extractor);

        await using var context = CreateContext();
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);
        Assert.Equal(ProcessingStatus.Failure, attempt.Status);
        Assert.Equal("Falha simulada na extração.", attempt.ErrorMessage);
    }

    private static async Task<int> SeedEdition(int edicao, RpiTipo tipo)
    {
        await using var context = CreateContext();
        var edition = new RpiEdition(edicao, tipo, DateTimeOffset.UtcNow);
        context.RpiEditions.Add(edition);
        await context.SaveChangesAsync();
        return edition.Id;
    }

    private static async Task Execute(RpiTipo tipo, int edicao, IRpiPublicationExtractor extractor)
    {
        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var publicationRepository = new PublicationRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var useCase = new ExtractRpiPublicationsUseCase(
            new FakeRpiFileStorage(), extractor, editionRepository, publicationRepository, attemptRepository, unitOfWork, new FakeTimeProvider());

        await useCase.ExecuteAsync(tipo, edicao, CancellationToken.None);
    }

    private static KodxRpiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KodxRpiDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new KodxRpiDbContext(options);
    }

    private sealed class FakeRpiFileStorage : IRpiFileStorage
    {
        public Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");

        public string GetPdfPath(RpiTipo tipo, int edicao) => throw new NotSupportedException("Não usado nestes testes.");

        public Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");

        public string GetTxtPath(RpiTipo tipo, int edicao) => throw new NotSupportedException("Não usado nestes testes.");

        public Task<string> ReadTxtAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) => Task.FromResult("texto fake");
    }

    private sealed class FakeExtractor(bool supported, IReadOnlyList<ExtractedPublication>? extracted = null, bool shouldThrow = false) : IRpiPublicationExtractor
    {
        public bool IsSupported(RpiTipo tipo) => supported;

        public IReadOnlyList<ExtractedPublication> Extract(RpiTipo tipo, string texto) =>
            shouldThrow ? throw new InvalidOperationException("Falha simulada na extração.") : extracted ?? [];
    }
}
