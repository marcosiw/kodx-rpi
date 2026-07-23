using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kodx.Rpi.Api.Tests.Grpc;

/// <summary>
/// Sobe a Api real (Program.cs) em memória (TestServer). Substitui a fila de background e o
/// Blob Storage por fakes — os testes de gRPC não devem depender do INPI/Azure reais — mas
/// mantém o Postgres real (mesmo docker-compose/env var já usado pelos outros testes de
/// integração do projeto).
///
/// Transporte em memória (TestServer), não Kestrel real: o handshake TLS/mTLS configurado em
/// Program.cs via ConfigureKestrel (Grpc:Mtls) não é exercido aqui — mesma classe de ponto cego
/// já documentada em ai/context.md pro bug de multiplexing h2c da fase 8. Cobertura real de
/// TLS/mTLS é manual, via grpcurl (ver ai/context.md).
/// </summary>
public sealed class RpiApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";

    public FakeBackgroundTaskQueue Queue { get; } = new();
    public FakeRpiBlobStorage BlobStorage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey:Value"] = ApiKey
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBackgroundTaskQueue>();
            services.AddSingleton<IBackgroundTaskQueue>(Queue);

            services.RemoveAll<IRpiBlobStorage>();
            services.AddSingleton<IRpiBlobStorage>(BlobStorage);
        });
    }

    public KodxRpiDbContext CreateDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5432;Database=kodx_rpi;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<KodxRpiDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new KodxRpiDbContext(options);
    }
}
