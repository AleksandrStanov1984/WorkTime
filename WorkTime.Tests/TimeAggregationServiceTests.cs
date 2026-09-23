using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;
using WorkTime.Services;

namespace WorkTime.Tests;

public sealed class TimeAggregationServiceTests
{
    [Fact]
    public async Task SingleSession_ReturnsWorkedTimeForDay()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db);

        await AddSessionAsync(
            db,
            project.Id,
            new DateTime(2026, 9, 23, 9, 0, 0),
            new DateTime(2026, 9, 23, 17, 0, 0));

        var service = new TimeAggregationService(
            db,
            new TestClock(new DateTime(2026, 9, 23, 20, 0, 0)));

        var result = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 23));

        Assert.Equal(TimeSpan.FromHours(8), result);
    }

    [Fact]
    public async Task MultipleSessions_SumsSameCalendarDay()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db);

        await AddSessionAsync(
            db,
            project.Id,
            new DateTime(2026, 9, 23, 8, 0, 0),
            new DateTime(2026, 9, 23, 12, 0, 0));

        await AddSessionAsync(
            db,
            project.Id,
            new DateTime(2026, 9, 23, 14, 0, 0),
            new DateTime(2026, 9, 23, 18, 30, 0));

        var service = new TimeAggregationService(
            db,
            new TestClock(new DateTime(2026, 9, 23, 21, 0, 0)));

        var result = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 23));

        Assert.Equal(TimeSpan.FromHours(8.5), result);
    }

    [Fact]
    public async Task SessionCrossingMidnight_IsSplitBetweenDays()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db);

        await AddSessionAsync(
            db,
            project.Id,
            new DateTime(2026, 9, 23, 22, 0, 0),
            new DateTime(2026, 9, 24, 3, 0, 0));

        var service = new TimeAggregationService(
            db,
            new TestClock(new DateTime(2026, 9, 24, 10, 0, 0)));

        var firstDay = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 23));

        var secondDay = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 24));

        Assert.Equal(TimeSpan.FromHours(2), firstDay);
        Assert.Equal(TimeSpan.FromHours(3), secondDay);
    }

    [Fact]
    public async Task MultiDaySession_IsLimitedToEachCalendarDay()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db);

        await AddSessionAsync(
            db,
            project.Id,
            new DateTime(2026, 9, 23, 20, 0, 0),
            new DateTime(2026, 9, 25, 4, 0, 0));

        var service = new TimeAggregationService(
            db,
            new TestClock(new DateTime(2026, 9, 25, 10, 0, 0)));

        var day23 = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 23));

        var day24 = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 24));

        var day25 = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 25));

        Assert.Equal(TimeSpan.FromHours(4), day23);
        Assert.Equal(TimeSpan.FromHours(24), day24);
        Assert.Equal(TimeSpan.FromHours(4), day25);
    }

    [Fact]
    public async Task ActiveSession_UsesCurrentTime()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db);

        await AddSessionAsync(
            db,
            project.Id,
            new DateTime(2026, 9, 23, 9, 0, 0),
            null);

        var service = new TimeAggregationService(
            db,
            new TestClock(new DateTime(2026, 9, 23, 12, 30, 0)));

        var result = await service.GetWorkedTimeForDayAsync(
            new DateTime(2026, 9, 23));

        Assert.Equal(TimeSpan.FromHours(3.5), result);
    }



    private static WorkTimeDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<WorkTimeDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

        var db = new WorkTimeDbContext(options);

        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        return db;
    }

    private static async Task<Project> CreateProjectAsync(
        WorkTimeDbContext db)
    {
        var project = new Project
        {
            Name = "Test Project",
            HourlyRate = 25m
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return project;
    }

    private static async Task AddSessionAsync(
        WorkTimeDbContext db,
        int projectId,
        DateTime startedAt,
        DateTime? endedAt)
    {
        db.WorkSessions.Add(new WorkSession
        {
            ProjectId = projectId,
            StartedAt = startedAt,
            EndedAt = endedAt
        });

        await db.SaveChangesAsync();
    }

    private sealed class TestClock : IClock
    {
        public TestClock(DateTime now)
        {
            Now = now;
        }

        public DateTime Now { get; set; }
    }
}