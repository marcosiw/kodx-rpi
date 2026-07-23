namespace Kodx.Rpi.Infrastructure.BackgroundProcessing;

public sealed class RpiWorkerOptions
{
    public const string SectionName = "RpiWorker";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromHours(1);
}
