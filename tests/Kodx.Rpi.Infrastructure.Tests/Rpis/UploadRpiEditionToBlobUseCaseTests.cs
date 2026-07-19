using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente. Usa um blob storage fake — o upload real via Azure.Storage.Blobs é validado manualmente contra o Azure real (sem emulador no projeto).</summary>
public sealed class UploadRpiEditionToBlobUseCaseTests : IAsyncLifetime
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
    public async Task Upload_com_sucesso_grava_tentativa_de_sucesso_e_envia_pdf_e_txt()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var blobStorage = new FakeRpiBlobStorage();

        var editionId = await SeedEdition(edicao);
        await Execute(edicao, blobStorage);

        await using var context = CreateContext();
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);

        Assert.Equal(ProcessingStatus.Success, attempt.Status);
        Assert.Equal(ProcessingStage.UploadBlob, attempt.Stage);
        Assert.True(blobStorage.PdfUploaded);
        Assert.True(blobStorage.TxtUploaded);
    }

    [Fact]
    public async Task Upload_com_falha_grava_tentativa_de_falha_sem_lancar_excecao()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        var blobStorage = new FakeRpiBlobStorage(shouldFail: true);

        var editionId = await SeedEdition(edicao);
        await Execute(edicao, blobStorage);

        await using var context = CreateContext();
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == editionId);

        Assert.Equal(ProcessingStatus.Failure, attempt.Status);
        Assert.Equal("Azure fora do ar (simulado).", attempt.ErrorMessage);
    }

    private static async Task<int> SeedEdition(int edicao)
    {
        await using var context = CreateContext();
        var edition = new RpiEdition(edicao, RpiTipo.Patentes, DateTimeOffset.UtcNow);
        context.RpiEditions.Add(edition);
        await context.SaveChangesAsync();
        return edition.Id;
    }

    private static async Task Execute(int edicao, IRpiBlobStorage blobStorage)
    {
        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var useCase = new UploadRpiEditionToBlobUseCase(
            new FakeRpiFileStorage(), blobStorage, editionRepository, attemptRepository, unitOfWork, new FakeTimeProvider());

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

    private sealed class FakeRpiFileStorage : IRpiFileStorage
    {
        public Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");

        public string GetPdfPath(RpiTipo tipo, int edicao) => "caminho-fake.pdf";

        public Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");

        public string GetTxtPath(RpiTipo tipo, int edicao) => "caminho-fake.txt";

        public Task<string> ReadTxtAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");
    }

    private sealed class FakeRpiBlobStorage(bool shouldFail = false) : IRpiBlobStorage
    {
        public bool PdfUploaded { get; private set; }
        public bool TxtUploaded { get; private set; }

        public Task UploadPdfAsync(RpiTipo tipo, int edicao, string localPdfPath, CancellationToken cancellationToken)
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("Azure fora do ar (simulado).");
            }

            PdfUploaded = true;
            return Task.CompletedTask;
        }

        public Task UploadTxtAsync(RpiTipo tipo, int edicao, string localTxtPath, CancellationToken cancellationToken)
        {
            TxtUploaded = true;
            return Task.CompletedTask;
        }
    }
}
