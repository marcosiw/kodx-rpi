namespace Kodx.Rpi.Domain.Rpis;

public sealed class RpiProcessingAttempt
{
    public int Id { get; private set; }
    public int RpiEditionId { get; private set; }
    public ProcessingStage Stage { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset FinishedAt { get; private set; }

    private RpiProcessingAttempt()
    {
    }

    private RpiProcessingAttempt(
        int rpiEditionId,
        ProcessingStage stage,
        ProcessingStatus status,
        string? errorMessage,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt)
    {
        RpiEditionId = rpiEditionId;
        Stage = stage;
        Status = status;
        ErrorMessage = errorMessage;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    public static RpiProcessingAttempt Success(int rpiEditionId, ProcessingStage stage, DateTimeOffset startedAt, DateTimeOffset finishedAt) =>
        new(rpiEditionId, stage, ProcessingStatus.Success, errorMessage: null, startedAt, finishedAt);

    public static RpiProcessingAttempt Failure(int rpiEditionId, ProcessingStage stage, string errorMessage, DateTimeOffset startedAt, DateTimeOffset finishedAt) =>
        new(rpiEditionId, stage, ProcessingStatus.Failure, errorMessage, startedAt, finishedAt);
}
