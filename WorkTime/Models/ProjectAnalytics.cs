namespace WorkTime.Models;

public sealed class ProjectAnalytics
{
    public TimeSpan WorkedTime { get; init; }

    public decimal HourlyRate { get; init; }

    public decimal EarnedAmount { get; init; }

    public decimal? AgreedPrice { get; init; }

    public decimal? RemainingAmount { get; init; }

    public decimal? ProgressPercent { get; init; }

    public decimal? EffectiveHourlyRate { get; init; }
}