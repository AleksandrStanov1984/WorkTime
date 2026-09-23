using Microsoft.EntityFrameworkCore;
using WorkTime.Data;

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

    public async Task<TimeSpan> GetWorkedTimeForDayAsync(DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sessions = await _dbContext.WorkSessions
            .Where(x =>
                x.StartedAt < dayEnd &&
                (x.EndedAt == null || x.EndedAt > dayStart))
            .ToListAsync();

        var total = TimeSpan.Zero;
        var now = _clock.Now;

        foreach (var session in sessions)
        {
            var sessionEnd = session.EndedAt ?? now;

            var effectiveStart =
                session.StartedAt < dayStart
                    ? dayStart
                    : session.StartedAt;

            var effectiveEnd =
                sessionEnd > dayEnd
                    ? dayEnd
                    : sessionEnd;

            if (effectiveEnd > effectiveStart)
            {
                total += effectiveEnd - effectiveStart;
            }
        }

        return total;
    }
}