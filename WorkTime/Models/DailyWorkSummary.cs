namespace WorkTime.Models;

public sealed class DailyWorkSummary
{
    public DateTime Date { get; init; }

    public TimeSpan WorkedTime { get; init; }
}