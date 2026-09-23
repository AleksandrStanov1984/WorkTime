using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;

namespace WorkTime.Services;

public sealed class WorkTimerService
{
    private readonly WorkTimeDbContext _dbContext;
    private readonly IClock _clock;

    public WorkTimerService(WorkTimeDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public WorkTimerState State { get; private set; } = WorkTimerState.Stopped;

    public int? ActiveProjectId { get; private set; }

    public WorkSession? ActiveSession { get; private set; }

    public async Task RestoreAsync()
    {
        var activeSessions = await _dbContext.WorkSessions
            .Where(x => x.EndedAt == null)
            .OrderBy(x => x.StartedAt)
            .ToListAsync();

        if (activeSessions.Count > 1)
        {
            throw new InvalidOperationException(
                "More than one active WorkSession exists.");
        }

        ActiveSession = activeSessions.SingleOrDefault();

        if (ActiveSession is null)
        {
            State = WorkTimerState.Stopped;
            ActiveProjectId = null;
            return;
        }

        ActiveProjectId = ActiveSession.ProjectId;
        State = WorkTimerState.Running;
    }

    public async Task StartAsync(int projectId)
    {
        if (State != WorkTimerState.Stopped)
        {
            throw new InvalidOperationException(
                "The timer can only be started from the Stopped state.");
        }

        await EnsureProjectCanBeUsedAsync(projectId);
        await EnsureNoActiveSessionExistsAsync();

        ActiveSession = new WorkSession
        {
            ProjectId = projectId,
            StartedAt = _clock.Now
        };

        _dbContext.WorkSessions.Add(ActiveSession);
        await _dbContext.SaveChangesAsync();

        ActiveProjectId = projectId;
        State = WorkTimerState.Running;
    }

    public async Task PauseAsync()
    {
        EnsureRunning();

        ActiveSession!.EndedAt = _clock.Now;
        await _dbContext.SaveChangesAsync();

        ActiveSession = null;
        State = WorkTimerState.Paused;
    }

    public async Task ResumeAsync()
    {
        if (State != WorkTimerState.Paused || ActiveProjectId is null)
        {
            throw new InvalidOperationException(
                "The timer can only be resumed from the Paused state.");
        }

        await EnsureProjectCanBeUsedAsync(ActiveProjectId.Value);
        await EnsureNoActiveSessionExistsAsync();

        ActiveSession = new WorkSession
        {
            ProjectId = ActiveProjectId.Value,
            StartedAt = _clock.Now
        };

        _dbContext.WorkSessions.Add(ActiveSession);
        await _dbContext.SaveChangesAsync();

        State = WorkTimerState.Running;
    }

    public async Task FinishAsync()
    {
        if (State == WorkTimerState.Stopped)
        {
            return;
        }

        if (State == WorkTimerState.Running)
        {
            ActiveSession!.EndedAt = _clock.Now;
            await _dbContext.SaveChangesAsync();
            ActiveSession = null;
        }

        ActiveProjectId = null;
        State = WorkTimerState.Stopped;
    }

    public async Task SwitchProjectAsync(int projectId)
    {
        EnsureRunning();

        if (ActiveProjectId == projectId)
        {
            return;
        }

        await EnsureProjectCanBeUsedAsync(projectId);

        var switchedAt = _clock.Now;

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync();

        ActiveSession!.EndedAt = switchedAt;

        var newSession = new WorkSession
        {
            ProjectId = projectId,
            StartedAt = switchedAt
        };

        _dbContext.WorkSessions.Add(newSession);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        ActiveSession = newSession;
        ActiveProjectId = projectId;
    }

    private void EnsureRunning()
    {
        if (State != WorkTimerState.Running || ActiveSession is null)
        {
            throw new InvalidOperationException(
                "The timer is not currently running.");
        }
    }

    private async Task EnsureProjectCanBeUsedAsync(int projectId)
    {
        var exists = await _dbContext.Projects
            .AnyAsync(x => x.Id == projectId && !x.IsArchived);

        if (!exists)
        {
            throw new InvalidOperationException(
                "The selected project does not exist or is archived.");
        }
    }

    private async Task EnsureNoActiveSessionExistsAsync()
    {
        if (await _dbContext.WorkSessions.AnyAsync(x => x.EndedAt == null))
        {
            throw new InvalidOperationException(
                "An active WorkSession already exists.");
        }
    }
}