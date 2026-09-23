using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;

namespace WorkTime.Services;

public sealed class ProjectAnalyticsService
{
    private readonly WorkTimeDbContext _dbContext;
    private readonly IClock _clock;

    public ProjectAnalyticsService(
        WorkTimeDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<ProjectAnalytics> GetAsync(
        int projectId)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .SingleAsync(x => x.Id == projectId);

        var sessions = await _dbContext.WorkSessions
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .ToListAsync();

        var workedTime = TimeSpan.Zero;
        var now = _clock.Now;

        foreach (var session in sessions)
        {
            var end = session.EndedAt ?? now;

            if (end > session.StartedAt)
            {
                workedTime += end - session.StartedAt;
            }
        }

        var workedHours =
            (decimal)workedTime.TotalHours;

        var earnedAmount = decimal.Round(
            workedHours * project.HourlyRate,
            2,
            MidpointRounding.AwayFromZero);

        decimal? remainingAmount = null;
        decimal? progressPercent = null;
        decimal? effectiveHourlyRate = null;

        if (project.AgreedPrice is not null)
        {
            remainingAmount = decimal.Max(
                0,
                project.AgreedPrice.Value -
                earnedAmount);

            if (project.AgreedPrice.Value > 0)
            {
                progressPercent = decimal.Round(
                    earnedAmount /
                    project.AgreedPrice.Value *
                    100,
                    1,
                    MidpointRounding.AwayFromZero);
            }

            if (workedHours > 0)
            {
                effectiveHourlyRate = decimal.Round(
                    project.AgreedPrice.Value /
                    workedHours,
                    2,
                    MidpointRounding.AwayFromZero);
            }
        }

        return new ProjectAnalytics
        {
            WorkedTime = workedTime,
            HourlyRate = project.HourlyRate,
            EarnedAmount = earnedAmount,
            AgreedPrice = project.AgreedPrice,
            RemainingAmount = remainingAmount,
            ProgressPercent = progressPercent,
            EffectiveHourlyRate = effectiveHourlyRate
        };
    }
}