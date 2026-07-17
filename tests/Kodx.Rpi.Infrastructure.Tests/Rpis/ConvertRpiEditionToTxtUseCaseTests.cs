using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente. Usa um extrator de PDF fake — a extração real via PdfPig é coberta por Pdf/PdfTextExtractorTests.cs.</summary>
public sealed class ConvertRpiEditionToTxtUseCaseTests : IAsyncLifetime
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
    public async Task Conversao_com_sucesso_grava_tentativa_de_sucesso_e_salva_txt()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var extractor = new FakeExtractor(text: "conteúdo extraído do pdf");
        var storage = new FakeRpiFileStorage();

        var editionId = await SeedEdition(edicao);
        await Execute(edicao, extractor, storage);

        await using var context = CreateContext();
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);

        Assert.Equal(ProcessingStatus.Success, attempt.Status);
        Assert.Equal(ProcessingStage.ConvertToTxt, attempt.Stage);
        Assert.Equal("conteúdo extraído do pdf", storage.SavedText);
    }

    [Fact]
    public async Task Conversao_com_pdf_corrompido_grava_tentativa_de_falha()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var extractor = new FakeExtractor(exception: new InvalidOperationException("PDF corrompido (simulado)."));
        var storage = new FakeRpiFileStorage();

        var editionId = await SeedEdition(edicao);
        await Execute(edicao, extractor, storage);

        await using var context = CreateContext();
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);

        Assert.Equal(ProcessingStatus.Failure, attempt.Status);
        Assert.Equal("PDF corrompido (simulado).", attempt.ErrorMessage);
        Assert.Null(storage.SavedText);
    }

    [Fact]
    public async Task Conversao_com_texto_vazio_grava_tentativa_de_falha()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var extractor = new FakeExtractor(text: "   ");
        var storage = new FakeRpiFileStorage();

        var editionId = await SeedEdition(edicao);
        await Execute(edicao, extractor, storage);

        await using var context = CreateContext();
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);

        Assert.Equal(ProcessingStatus.Failure, attempt.Status);
        Assert.Null(storage.SavedText);
    }

    private static async Task<int> SeedEdition(int edicao)
    {
        await using var context = CreateContext();
        var edition = new RpiEdition(edicao, RpiTipo.Patentes, DateTimeOffset.UtcNow);
        context.RpiEditions.Add(edition);
        await context.SaveChangesAsync();
        return edition.Id;
    }

    private static async Task Execute(int edicao, IPdfTextExtractor extractor, IRpiFileStorage storage)
    {
        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var useCase = new ConvertRpiEditionToTxtUseCase(
            storage, extractor, editionRepository, attemptRepository, unitOfWork, new FakeTimeProvider());

        await useCase.ExecuteAsync(RpiTipo.Patentes, edicao, CancellationToken.None);
    }

    private static KodxRpiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KodxRpiDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new KodxRpiDbContext(options);
    }

    private sealed class FakeExtractor(string? text = null, Exception? exception = null) : IPdfTextExtractor
    {
        public string ExtractText(string pdfPath) => exception is not null ? throw exception : text!;
    }

    private sealed class FakeRpiFileStorage : IRpiFileStorage
    {
        public string? SavedText { get; private set; }

        public Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");

        public string GetPdfPath(RpiTipo tipo, int edicao) => "caminho-fake.pdf";

        public Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken)
        {
            SavedText = content;
            return Task.CompletedTask;
        }
    }
}
