using Azure.Storage.Blobs;
using Kodx.Rpi.Api.Security;
using Kodx.Rpi.Application;
using Kodx.Rpi.Application.Rpis;
using Kodx.Rpi.Domain.Rpis;
using Kodx.Rpi.Infrastructure.BackgroundProcessing;
using Kodx.Rpi.Infrastructure.Persistence;
using Kodx.Rpi.Infrastructure.Rpis;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();

    if (context.HostingEnvironment.IsDevelopment())
    {
        configuration.WriteTo.Console();
    }
    else
    {
        configuration.WriteTo.Console(new CompactJsonFormatter());
    }
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName));

builder.Services.AddDbContext<KodxRpiDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Postgres"))
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRpiEditionRepository, RpiEditionRepository>();
builder.Services.AddScoped<IRpiProcessingAttemptRepository, RpiProcessingAttemptRepository>();
builder.Services.AddScoped<DownloadRpiEditionUseCase>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<InpiOptions>().Bind(builder.Configuration.GetSection(InpiOptions.SectionName));
builder.Services.AddOptions<RpiStorageOptions>().Bind(builder.Configuration.GetSection(RpiStorageOptions.SectionName));
builder.Services.AddScoped<IRpiFileStorage, LocalDiskRpiFileStorage>();
builder.Services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();
builder.Services.AddScoped<ConvertRpiEditionToTxtUseCase>();

builder.Services.AddOptions<RpiBlobStorageOptions>().Bind(builder.Configuration.GetSection(RpiBlobStorageOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RpiBlobStorageOptions>>().Value;
    return new BlobServiceClient(options.ConnectionString);
});
builder.Services.AddScoped<IRpiBlobStorage, AzureBlobRpiStorage>();
builder.Services.AddScoped<UploadRpiEditionToBlobUseCase>();

builder.Services.AddScoped<IRpiPublicationExtractor, RpiPublicationExtractor>();
builder.Services.AddScoped<IPublicationRepository, PublicationRepository>();
builder.Services.AddScoped<ExtractRpiPublicationsUseCase>();

builder.Services.AddHttpClient<IRpiDownloader, InpiRpiDownloader>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InpiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

builder.Services.AddHttpClient<IRpiCalendar, InpiRpiCalendar>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<InpiOptions>>().Value;
    client.BaseAddress = new Uri(options.CalendarUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var anchorEdition = config.GetValue<int>("RpiSchedule:AnchorEdition");
    var anchorPublicationDate = config.GetValue<DateOnly>("RpiSchedule:AnchorPublicationDate");
    return new RpiEditionCalculator(sp.GetRequiredService<TimeProvider>(), anchorEdition, anchorPublicationDate);
});

builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRequestTimeouts();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
