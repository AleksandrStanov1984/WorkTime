namespace WorkTime.Models;

public sealed class HistorySummary
{
    public TimeSpan WorkedTime { get; init; }

    public TimeSpan PauseTime { get; init; }

    public decimal HourlyRate { get; init; }

    public decimal Amount { get; init; }
}