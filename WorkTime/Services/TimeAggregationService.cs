using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;

namespace WorkTime.Services;

public sealed class TimeAggregationService
{
    private readonly WorkTimeDbContext _dbContext;
    private readonly IClock _clock;

    public TimeAggregationService(
        WorkTimeDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<TimeSpan> GetWorkedTimeForDayAsync(
        DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        var sessions = await GetSessionsAsync(
            start,
            end,
            null);

        return CalculateDuration(
            sessions.Select(x =>
                (x.StartedAt, x.EndedAt)),
            start,
            end);
    }

    public async Task<List<DailyWorkSummary>>
        GetDailyWorkedTimeAsync(
            DateTime from,
            DateTime to)
    {
        var rangeStart = from.Date;
        var rangeEnd = to.Date.AddDays(1);

        if (rangeEnd <= rangeStart)
        {
            return [];
        }

        var sessions = await GetSessionsAsync(
            rangeStart,
            rangeEnd,
            null);

        var result =
            new List<DailyWorkSummary>();

        for (var day = rangeStart;
             day < rangeEnd;
             day = day.AddDays(1))
        {
            var dayEnd = day.AddDays(1);

            result.Add(new DailyWorkSummary
            {
                Date = day,
                WorkedTime = CalculateDuration(
                    sessions.Select(x =>
                        (x.StartedAt, x.EndedAt)),
                    day,
                    dayEnd)
            });
        }

        return result;
    }

    public async Task<HistorySummary> GetHistorySummaryAsync(
        int projectId,
        DateTime selectedDate,
        HistoryPeriod period)
    {
        var (start, end) =
            GetPeriodRange(selectedDate, period);

        var project = await _dbContext.Projects
            .AsNoTracking()
            .SingleAsync(x => x.Id == projectId);

        var sessions = await GetSessionsAsync(
            start,
            end,
            projectId);

        var pauses = await _dbContext.PauseIntervals
            .AsNoTracking()
            .Where(x =>
                x.ProjectId == projectId &&
                x.StartedAt < end &&
                (x.EndedAt == null ||
                 x.EndedAt > start))
            .ToListAsync();

        var workedTime = CalculateDuration(
            sessions.Select(x =>
                (x.StartedAt, x.EndedAt)),
            start,
            end);

        var pauseTime = CalculateDuration(
            pauses.Select(x =>
                (x.StartedAt, x.EndedAt)),
            start,
            end);

        var amount =
            (decimal)workedTime.TotalHours *
            project.HourlyRate;

        return new HistorySummary
        {
            WorkedTime = workedTime,
            PauseTime = pauseTime,
            HourlyRate = project.HourlyRate,
            Amount = decimal.Round(
                amount,
                2,
                MidpointRounding.AwayFromZero)
        };
    }

    public static (DateTime Start, DateTime End)
        GetPeriodRange(
            DateTime selectedDate,
            HistoryPeriod period)
    {
        var date = selectedDate.Date;

        return period switch
        {
            HistoryPeriod.Day =>
                (date, date.AddDays(1)),

            HistoryPeriod.Week =>
                GetWeekRange(date),

            HistoryPeriod.Month =>
                (
                    new DateTime(
                        date.Year,
                        date.Month,
                        1),
                    new DateTime(
                        date.Year,
                        date.Month,
                        1).AddMonths(1)
                ),

            _ => throw new ArgumentOutOfRangeException(
                nameof(period))
        };
    }

    private static (DateTime Start, DateTime End)
        GetWeekRange(DateTime date)
    {
        var daysSinceMonday =
            ((int)date.DayOfWeek + 6) % 7;

        var start =
            date.AddDays(-daysSinceMonday);

        return (
            start,
            start.AddDays(7));
    }

    private async Task<List<WorkSession>>
        GetSessionsAsync(
            DateTime start,
            DateTime end,
            int? projectId)
    {
        var query = _dbContext.WorkSessions
            .AsNoTracking()
            .Where(x =>
                x.StartedAt < end &&
                (x.EndedAt == null ||
                 x.EndedAt > start));

        if (projectId is not null)
        {
            query = query.Where(
                x => x.ProjectId == projectId);
        }

        return await query.ToListAsync();
    }

    private TimeSpan CalculateDuration(
        IEnumerable<(DateTime StartedAt, DateTime? EndedAt)>
            intervals,
        DateTime rangeStart,
        DateTime rangeEnd)
    {
        var total = TimeSpan.Zero;
        var now = _clock.Now;

        foreach (var interval in intervals)
        {
            var intervalEnd =
                interval.EndedAt ?? now;

            var effectiveStart =
                interval.StartedAt < rangeStart
                    ? rangeStart
                    : interval.StartedAt;

            var effectiveEnd =
                intervalEnd > rangeEnd
                    ? rangeEnd
                    : intervalEnd;

            if (effectiveEnd > effectiveStart)
            {
                total +=
                    effectiveEnd - effectiveStart;
            }
        }

        return total;
    }

    public async Task<TimeSpan> GetWorkedTimeForProjectDayAsync(
    int projectId,
    DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sessions = await GetSessionsAsync(
            dayStart,
            dayEnd,
            projectId);

        return CalculateDuration(
            sessions.Select(x =>
                (x.StartedAt, x.EndedAt)),
            dayStart,
            dayEnd);
    }
}