using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Kodx.Rpi.Infrastructure.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente. Compõe os 4 use cases reais (fase 9, extraído do que antes era inline em RpiGrpcService.TriggerDownload) com fakes de I/O, cobrindo o curto-circuito da cadeia.</summary>
public sealed class RunRpiPipelineUseCaseTests : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
        ?? "Host=localhost;Port=5432;Database=kodx_rpi;Username=postgres;Password=postgres";

    private static readonly DateOnly AnchorDate = new(2026, 6, 2);
    private const int AnchorEdition = 2891;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await TestDatabaseMigrator.MigrateAsync(context);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Falha_no_download_para_a_cadeia_sem_gravar_as_demais_etapas()
    {
        var edicao = AnchorEdition + Random.Shared.Next(1000, 100000);
        var storage = new FakeRpiFileStorage();

        var editionId = await Execute(edicao, downloadShouldSucceed: false, storage: storage);

        await using var context = CreateContext();
        var attempts = await context.RpiProcessingAttempts.Where(a => a.RpiEditionId == editionId).ToListAsync();

        var attempt = Assert.Single(attempts);
        Assert.Equal(ProcessingStage.Download, attempt.Stage);
        Assert.Equal(ProcessingStatus.Failure, attempt.Status);
        Assert.False(storage.PdfSaved);
    }

    [Fact]
    public async Task Falha_na_conversao_para_a_cadeia_antes_do_upload_e_da_extracao()
    {
        var edicao = AnchorEdition + Random.Shared.Next(1000, 100000);
        var storage = new FakeRpiFileStorage();

        var editionId = await Execute(edicao, downloadShouldSucceed: true, storage: storage, convertShouldSucceed: false);

        await using var context = CreateContext();
        var attempts = await context.RpiProcessingAttempts.Where(a => a.RpiEditionId == editionId).ToListAsync();

        Assert.Equal(2, attempts.Count);
        Assert.Contains(attempts, a => a.Stage == ProcessingStage.Download && a.Status == ProcessingStatus.Success);
        Assert.Contains(attempts, a => a.Stage == ProcessingStage.ConvertToTxt && a.Status == ProcessingStatus.Failure);
        Assert.DoesNotContain(attempts, a => a.Stage is ProcessingStage.UploadBlob or ProcessingStage.ExtractPublications);
    }

    [Fact]
    public async Task Sucesso_completo_grava_as_4_etapas_e_persiste_publicacoes()
    {
        var edicao = AnchorEdition + Random.Shared.Next(1000, 100000);
        var storage = new FakeRpiFileStorage();
        var extracted = new[] { new ExtractedPublication("PI 1111111-1", new PublicationPayload { Conteudo = "conteúdo 1" }) };

        var editionId = await Execute(edicao, downloadShouldSucceed: true, storage: storage, convertShouldSucceed: true, extracted: extracted);

        await using var context = CreateContext();
        var attempts = await context.RpiProcessingAttempts.Where(a => a.RpiEditionId == editionId).ToListAsync();

        Assert.Equal(4, attempts.Count);
        Assert.All(attempts, a => Assert.Equal(ProcessingStatus.Success, a.Status));

        var publicacoes = await context.Publications.Where(p => p.RpiEditionId == editionId).ToListAsync();
        Assert.Equal("PI 1111111-1", Assert.Single(publicacoes).Numero);
    }

    private static async Task<int> Execute(
        int edicao,
        bool downloadShouldSucceed,
        FakeRpiFileStorage storage,
        bool convertShouldSucceed = true,
        IReadOnlyList<ExtractedPublication>? extracted = null)
    {
        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var publicationRepository = new PublicationRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var calculator = new RpiEditionCalculator(TimeProvider.System, AnchorEdition, AnchorDate);

        var downloadUseCase = new DownloadRpiEditionUseCase(
            new FakeRpiDownloader(downloadShouldSucceed), storage, new FakeRpiCalendar(), editionRepository,
            attemptRepository, unitOfWork, calculator, new FakeTimeProvider());
        var convertUseCase = new ConvertRpiEditionToTxtUseCase(
            storage, new FakePdfTextExtractor(convertShouldSucceed), editionRepository, attemptRepository, unitOfWork, new FakeTimeProvider());
        var uploadUseCase = new UploadRpiEditionToBlobUseCase(
            storage, new FakeRpiBlobStorage(), editionRepository, attemptRepository, unitOfWork, new FakeTimeProvider());
        var extractUseCase = new ExtractRpiPublicationsUseCase(
            storage, new FakeRpiPublicationExtractor(extracted ?? []), editionRepository, publicationRepository, attemptRepository, unitOfWork, new FakeTimeProvider());

        var pipeline = new RunRpiPipelineUseCase(downloadUseCase, convertUseCase, uploadUseCase, extractUseCase);
        await pipeline.ExecuteAsync(RpiTipo.Patentes, edicao, CancellationToken.None);

        var edition = await context.RpiEditions.SingleAsync(e => e.Edicao == edicao && e.Tipo == RpiTipo.Patentes);
        return edition.Id;
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
        private string? _txt;

        public bool PdfSaved { get; private set; }

        public Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken)
        {
            PdfSaved = true;
            return Task.CompletedTask;
        }

        public string GetPdfPath(RpiTipo tipo, int edicao) => "caminho-fake.pdf";

        public Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken)
        {
            _txt = content;
            return Task.CompletedTask;
        }

        public string GetTxtPath(RpiTipo tipo, int edicao) => "caminho-fake.txt";

        public Task<string> ReadTxtAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
            Task.FromResult(_txt ?? "");
    }

    private sealed class FakeRpiDownloader(bool shouldSucceed) : IRpiDownloader
    {
        public Task<byte[]> DownloadAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
            shouldSucceed
                ? Task.FromResult("%PDF-1.4 conteúdo fake"u8.ToArray())
                : throw new RpiDownloadException("INPI fora do ar (simulado).");
    }

    private sealed class FakeRpiCalendar : IRpiCalendar
    {
        public Task<RpiCalendarEntry?> GetMostRecentEditionAsync(CancellationToken cancellationToken) =>
            Task.FromResult<RpiCalendarEntry?>(null);

        public Task<DateOnly?> GetPublicationDateAsync(int edicao, CancellationToken cancellationToken) =>
            Task.FromResult<DateOnly?>(null);
    }

    private sealed class FakePdfTextExtractor(bool shouldSucceed) : IPdfTextExtractor
    {
        public string ExtractText(string pdfPath) =>
            shouldSucceed ? "conteúdo extraído do pdf" : throw new InvalidOperationException("PDF corrompido (simulado).");
    }

    private sealed class FakeRpiBlobStorage : IRpiBlobStorage
    {
        public Task UploadPdfAsync(RpiTipo tipo, int edicao, string localPdfPath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task UploadTxtAsync(RpiTipo tipo, int edicao, string localTxtPath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Stream> DownloadPdfAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");
    }

    private sealed class FakeRpiPublicationExtractor(IReadOnlyList<ExtractedPublication> extracted) : IRpiPublicationExtractor
    {
        public bool IsSupported(RpiTipo tipo) => true;

        public IReadOnlyList<ExtractedPublication> Extract(RpiTipo tipo, string texto) => extracted;
    }
}
