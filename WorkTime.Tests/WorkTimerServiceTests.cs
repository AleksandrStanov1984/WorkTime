using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;
using WorkTime.Services;

namespace WorkTime.Tests;

public sealed class WorkTimerServiceTests
{
    [Fact]
    public async Task StartAndFinish_CreatesCompletedSession()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db, "Mary Salon");
        var clock = new TestClock(new DateTime(2026, 9, 23, 9, 0, 0));
        var service = new WorkTimerService(db, clock);

        await service.StartAsync(project.Id);

        Assert.Equal(WorkTimerState.Running, service.State);
        Assert.Equal(project.Id, service.ActiveProjectId);

        clock.Now = new DateTime(2026, 9, 23, 17, 0, 0);
        await service.FinishAsync();

        Assert.Equal(WorkTimerState.Stopped, service.State);
        Assert.Null(service.ActiveProjectId);
        Assert.Null(service.ActiveSession);

        var session = await db.WorkSessions.SingleAsync();

        Assert.Equal(
            new DateTime(2026, 9, 23, 9, 0, 0),
            session.StartedAt);

        Assert.Equal(
            new DateTime(2026, 9, 23, 17, 0, 0),
            session.EndedAt);
    }

    [Fact]
    public async Task PauseResumeFinish_ExcludesPausedTime()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db, "Mary Salon");
        var clock = new TestClock(new DateTime(2026, 9, 23, 9, 0, 0));
        var service = new WorkTimerService(db, clock);

        await service.StartAsync(project.Id);

        clock.Now = new DateTime(2026, 9, 23, 11, 0, 0);
        await service.PauseAsync();

        Assert.Equal(WorkTimerState.Paused, service.State);
        Assert.Null(service.ActiveSession);
        Assert.Equal(project.Id, service.ActiveProjectId);

        clock.Now = new DateTime(2026, 9, 23, 13, 0, 0);
        await service.ResumeAsync();

        clock.Now = new DateTime(2026, 9, 23, 18, 30, 0);
        await service.FinishAsync();

        var sessions = await db.WorkSessions
            .OrderBy(x => x.StartedAt)
            .ToListAsync();

        Assert.Equal(2, sessions.Count);

        Assert.Equal(
            new DateTime(2026, 9, 23, 9, 0, 0),
            sessions[0].StartedAt);

        Assert.Equal(
            new DateTime(2026, 9, 23, 11, 0, 0),
            sessions[0].EndedAt);

        Assert.Equal(
            new DateTime(2026, 9, 23, 13, 0, 0),
            sessions[1].StartedAt);

        Assert.Equal(
            new DateTime(2026, 9, 23, 18, 30, 0),
            sessions[1].EndedAt);

        var worked = sessions.Aggregate(
            TimeSpan.Zero,
            (total, session) =>
                total + (session.EndedAt!.Value - session.StartedAt));

        Assert.Equal(TimeSpan.FromHours(7.5), worked);
    }

    [Fact]
    public async Task SwitchProject_UsesSameTimestampForBothSessions()
    {
        await using var db = CreateDbContext();

        var mary = await CreateProjectAsync(db, "Mary Salon");
        var qrMenu = await CreateProjectAsync(db, "SA QR Menu");

        var clock = new TestClock(new DateTime(2026, 9, 23, 9, 0, 0));
        var service = new WorkTimerService(db, clock);

        await service.StartAsync(mary.Id);

        clock.Now = new DateTime(2026, 9, 23, 11, 20, 0);
        await service.SwitchProjectAsync(qrMenu.Id);

        var sessions = await db.WorkSessions
            .OrderBy(x => x.StartedAt)
            .ToListAsync();

        Assert.Equal(2, sessions.Count);

        Assert.Equal(mary.Id, sessions[0].ProjectId);
        Assert.Equal(qrMenu.Id, sessions[1].ProjectId);

        Assert.Equal(
            sessions[0].EndedAt,
            sessions[1].StartedAt);

        Assert.Equal(WorkTimerState.Running, service.State);
        Assert.Equal(qrMenu.Id, service.ActiveProjectId);
        Assert.Equal(qrMenu.Id, service.ActiveSession!.ProjectId);
    }

    [Fact]
    public async Task RestoreAsync_RestoresUnfinishedSession()
    {
        await using var db = CreateDbContext();
        var project = await CreateProjectAsync(db, "Mary Salon");

        db.WorkSessions.Add(new WorkSession
        {
            ProjectId = project.Id,
            StartedAt = new DateTime(2026, 9, 23, 20, 0, 0)
        });

        await db.SaveChangesAsync();

        var clock = new TestClock(new DateTime(2026, 9, 24, 8, 0, 0));
        var service = new WorkTimerService(db, clock);

        await service.RestoreAsync();

        Assert.Equal(WorkTimerState.Running, service.State);
        Assert.Equal(project.Id, service.ActiveProjectId);
        Assert.NotNull(service.ActiveSession);
        Assert.Null(service.ActiveSession!.EndedAt);
    }

    [Fact]
    public async Task StartAsync_WhenActiveSessionAlreadyExists_Throws()
    {
        await using var db = CreateDbContext();

        var firstProject =
            await CreateProjectAsync(db, "Mary Salon");

        var secondProject =
            await CreateProjectAsync(db, "SA QR Menu");

        db.WorkSessions.Add(new WorkSession
        {
            ProjectId = firstProject.Id,
            StartedAt = new DateTime(2026, 9, 23, 9, 0, 0)
        });

        await db.SaveChangesAsync();

        var clock = new TestClock(new DateTime(2026, 9, 23, 10, 0, 0));
        var service = new WorkTimerService(db, clock);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StartAsync(secondProject.Id));

        Assert.Single(await db.WorkSessions.ToListAsync());
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
        WorkTimeDbContext db,
        string name)
    {
        var project = new Project
        {
            Name = name,
            HourlyRate = 25m
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return project;
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