using Kodx.Rpi.Application;
using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente.</summary>
public sealed class DownloadRpiEditionUseCaseTests : IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
        ?? "Host=localhost;Port=5432;Database=kodx_rpi;Username=postgres;Password=postgres";

    private static readonly DateOnly AnchorDate = new(2026, 6, 2);
    private const int AnchorEdition = 2891;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Download_com_sucesso_grava_edicao_e_tentativa_de_sucesso()
    {
        var edicao = AnchorEdition + Random.Shared.Next(1000, 100000);
        var downloader = new FakeRpiDownloader(shouldSucceed: true);
        var storage = new FakeRpiFileStorage();

        await Execute(edicao, downloader, storage);

        await using var context = CreateContext();
        var savedEdition = await context.RpiEditions.SingleAsync(e => e.Edicao == edicao && e.Tipo == RpiTipo.Patentes);
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == savedEdition.Id);

        Assert.Equal(ProcessingStatus.Success, attempt.Status);
        Assert.Equal(ProcessingStage.Download, attempt.Stage);
        Assert.True(storage.Saved);
    }

    [Fact]
    public async Task Download_com_falha_grava_tentativa_de_falha_sem_lancar_excecao()
    {
        var edicao = AnchorEdition + Random.Shared.Next(1000, 100000);
        var downloader = new FakeRpiDownloader(shouldSucceed: false);
        var storage = new FakeRpiFileStorage();

        await Execute(edicao, downloader, storage);

        await using var context = CreateContext();
        var savedEdition = await context.RpiEditions.SingleAsync(e => e.Edicao == edicao && e.Tipo == RpiTipo.Patentes);
        var attempt = await context.RpiProcessingAttempts.SingleAsync(a => a.RpiEditionId == savedEdition.Id);

        Assert.Equal(ProcessingStatus.Failure, attempt.Status);
        Assert.Equal("INPI fora do ar (simulado).", attempt.ErrorMessage);
        Assert.False(storage.Saved);
    }

    private static async Task Execute(int edicao, IRpiDownloader downloader, IRpiFileStorage storage)
    {
        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var unitOfWork = new UnitOfWork(context);
        var calculator = new RpiEditionCalculator(TimeProvider.System, AnchorEdition, AnchorDate);

        var useCase = new DownloadRpiEditionUseCase(
            downloader, storage, new FakeRpiCalendar(), editionRepository, attemptRepository, unitOfWork, calculator, new FakeTimeProvider());

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

    private sealed class FakeRpiDownloader(bool shouldSucceed) : IRpiDownloader
    {
        public Task<byte[]> DownloadAsync(RpiTipo tipo, int edicao, CancellationToken cancellationToken)
        {
            if (!shouldSucceed)
            {
                throw new RpiDownloadException("INPI fora do ar (simulado).");
            }

            return Task.FromResult("%PDF-1.4 conteúdo fake"u8.ToArray());
        }
    }

    private sealed class FakeRpiFileStorage : IRpiFileStorage
    {
        public bool Saved { get; private set; }

        public Task SavePdfAsync(RpiTipo tipo, int edicao, byte[] content, CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }

        public string GetPdfPath(RpiTipo tipo, int edicao) => throw new NotSupportedException("Não usado nestes testes.");

        public Task SaveTxtAsync(RpiTipo tipo, int edicao, string content, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");
    }

    /// <summary>Simula o calendário indisponível, forçando o fallback pro cálculo por âncora (é o que estes testes exercitam).</summary>
    private sealed class FakeRpiCalendar : IRpiCalendar
    {
        public Task<RpiCalendarEntry?> GetMostRecentEditionAsync(CancellationToken cancellationToken) =>
            Task.FromResult<RpiCalendarEntry?>(null);

        public Task<DateOnly?> GetPublicationDateAsync(int edicao, CancellationToken cancellationToken) =>
            Task.FromResult<DateOnly?>(null);
    }
}
