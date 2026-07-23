using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Kodx.Rpi.Infrastructure.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kodx.Rpi.Infrastructure.Tests.Rpis;

/// <summary>Testes de integração: exigem o Postgres do docker-compose.yml rodando localmente. Cobre a decisão de "o que falta processar" chamada pelo worker (fase 9, ver ai/context.md) a cada tick.</summary>
public sealed class ProcessDueRpiEditionsUseCaseTests : IAsyncLifetime
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
    public async Task Tipo_sem_edicao_existente_esta_pendente()
    {
        var edicao = Random.Shared.Next(100000, 999999);

        var pendentes = await GetPendingTipos(edicao);

        Assert.Equal(8, pendentes.Count);
        Assert.Contains(RpiTipo.Patentes, pendentes);
    }

    [Fact]
    public async Task Tipo_suportado_com_extracao_bem_sucedida_nao_esta_pendente()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        await SeedAttempt(edicao, RpiTipo.Marcas, ProcessingStage.ExtractPublications, ProcessingStatus.Success);

        var pendentes = await GetPendingTipos(edicao);

        Assert.DoesNotContain(RpiTipo.Marcas, pendentes);
    }

    [Fact]
    public async Task Tipo_nao_suportado_com_upload_bem_sucedido_nao_esta_pendente()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        await SeedAttempt(edicao, RpiTipo.Comunicados, ProcessingStage.UploadBlob, ProcessingStatus.Success);

        var pendentes = await GetPendingTipos(edicao);

        Assert.DoesNotContain(RpiTipo.Comunicados, pendentes);
    }

    [Fact]
    public async Task Tipo_suportado_com_upload_mas_sem_extracao_continua_pendente()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        await SeedAttempt(edicao, RpiTipo.Patentes, ProcessingStage.UploadBlob, ProcessingStatus.Success);

        var pendentes = await GetPendingTipos(edicao);

        Assert.Contains(RpiTipo.Patentes, pendentes);
    }

    [Fact]
    public async Task ExecuteAsync_resolve_a_edicao_pelo_calendario_e_enfileira_os_pendentes()
    {
        var edicao = Random.Shared.Next(100000, 999999);
        await SeedAttempt(edicao, RpiTipo.Marcas, ProcessingStage.ExtractPublications, ProcessingStatus.Success);

        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var calculator = new RpiEditionCalculator(TimeProvider.System, edicao, DateOnly.FromDateTime(DateTime.UtcNow));
        var queue = new FakeBackgroundTaskQueue();

        var useCase = new ProcessDueRpiEditionsUseCase(
            new FakeRpiCalendar(edicao), calculator, editionRepository, attemptRepository,
            new FakeRpiPublicationExtractor(), queue, NullLogger<ProcessDueRpiEditionsUseCase>.Instance);

        await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(7, queue.EnqueuedCount);
    }

    private static async Task SeedAttempt(int edicao, RpiTipo tipo, ProcessingStage stage, ProcessingStatus status)
    {
        await using var context = CreateContext();
        var edition = new RpiEdition(edicao, tipo, DateTimeOffset.UtcNow);
        context.RpiEditions.Add(edition);
        await context.SaveChangesAsync();

        var attempt = status == ProcessingStatus.Success
            ? RpiProcessingAttempt.Success(edition.Id, stage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)
            : RpiProcessingAttempt.Failure(edition.Id, stage, "falha simulada", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        context.RpiProcessingAttempts.Add(attempt);
        await context.SaveChangesAsync();
    }

    private static async Task<IReadOnlyList<RpiTipo>> GetPendingTipos(int edicao)
    {
        await using var context = CreateContext();
        var editionRepository = new RpiEditionRepository(context);
        var attemptRepository = new RpiProcessingAttemptRepository(context);
        var calculator = new RpiEditionCalculator(TimeProvider.System, edicao, DateOnly.FromDateTime(DateTime.UtcNow));

        var useCase = new ProcessDueRpiEditionsUseCase(
            new FakeRpiCalendar(edicao), calculator, editionRepository, attemptRepository,
            new FakeRpiPublicationExtractor(), new FakeBackgroundTaskQueue(), NullLogger<ProcessDueRpiEditionsUseCase>.Instance);

        return await useCase.GetPendingTiposAsync(edicao, CancellationToken.None);
    }

    private static KodxRpiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<KodxRpiDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new KodxRpiDbContext(options);
    }

    private sealed class FakeRpiCalendar(int edicao) : IRpiCalendar
    {
        public Task<RpiCalendarEntry?> GetMostRecentEditionAsync(CancellationToken cancellationToken) =>
            Task.FromResult<RpiCalendarEntry?>(new RpiCalendarEntry(edicao, DateOnly.FromDateTime(DateTime.UtcNow)));

        public Task<DateOnly?> GetPublicationDateAsync(int edicao, CancellationToken cancellationToken) =>
            Task.FromResult<DateOnly?>(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>Mesma cobertura de RpiPublicationExtractor (Infrastructure) sem depender das regras reais por tipo.</summary>
    private sealed class FakeRpiPublicationExtractor : IRpiPublicationExtractor
    {
        public bool IsSupported(RpiTipo tipo) =>
            tipo is RpiTipo.Marcas or RpiTipo.Patentes or RpiTipo.DesenhosIndustriais or RpiTipo.ProgramasComputador;

        public IReadOnlyList<ExtractedPublication> Extract(RpiTipo tipo, string texto) => [];
    }

    private sealed class FakeBackgroundTaskQueue : IBackgroundTaskQueue
    {
        public int EnqueuedCount { get; private set; }

        public void Enqueue(Func<IServiceProvider, CancellationToken, Task> workItem) => EnqueuedCount++;

        public Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Não usado nestes testes.");
    }
}
