using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;

namespace WorkTime.Services;

public sealed class WorkTimerService
{
    private readonly WorkTimeDbContext _dbContext;
    private readonly IClock _clock;

    public WorkTimerService(
        WorkTimeDbContext dbContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public WorkTimerState State { get; private set; } =
        WorkTimerState.Stopped;

    public int? ActiveProjectId { get; private set; }

    public WorkSession? ActiveSession { get; private set; }

    public PauseInterval? ActivePause { get; private set; }

    public async Task RestoreAsync()
    {
        var activeSessions = await _dbContext.WorkSessions
            .Where(x => x.EndedAt == null)
            .OrderBy(x => x.StartedAt)
            .ToListAsync();

        var activePauses = await _dbContext.PauseIntervals
            .Where(x => x.EndedAt == null)
            .OrderBy(x => x.StartedAt)
            .ToListAsync();

        if (activeSessions.Count > 1)
        {
            throw new InvalidOperationException(
                "More than one active WorkSession exists.");
        }

        if (activePauses.Count > 1)
        {
            throw new InvalidOperationException(
                "More than one active PauseInterval exists.");
        }

        if (activeSessions.Count == 1 &&
            activePauses.Count == 1)
        {
            throw new InvalidOperationException(
                "A work session and pause cannot both be active.");
        }

        ActiveSession = activeSessions.SingleOrDefault();
        ActivePause = activePauses.SingleOrDefault();

        if (ActiveSession is not null)
        {
            ActiveProjectId = ActiveSession.ProjectId;
            State = WorkTimerState.Running;
            return;
        }

        if (ActivePause is not null)
        {
            ActiveProjectId = ActivePause.ProjectId;
            State = WorkTimerState.Paused;
            return;
        }

        ActiveProjectId = null;
        State = WorkTimerState.Stopped;
    }

    public async Task StartAsync(int projectId)
    {
        if (State != WorkTimerState.Stopped)
        {
            throw new InvalidOperationException(
                "The timer can only be started from the Stopped state.");
        }

        await EnsureProjectCanBeUsedAsync(projectId);
        await EnsureNothingActiveAsync();

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

        var pausedAt = _clock.Now;

        ActiveSession!.EndedAt = pausedAt;

        ActivePause = new PauseInterval
        {
            ProjectId = ActiveProjectId!.Value,
            StartedAt = pausedAt
        };

        _dbContext.PauseIntervals.Add(ActivePause);

        await _dbContext.SaveChangesAsync();

        ActiveSession = null;
        State = WorkTimerState.Paused;
    }

    public async Task ResumeAsync()
    {
        if (State != WorkTimerState.Paused ||
            ActiveProjectId is null ||
            ActivePause is null)
        {
            throw new InvalidOperationException(
                "The timer can only be resumed from the Paused state.");
        }

        await EnsureProjectCanBeUsedAsync(
            ActiveProjectId.Value);

        var resumedAt = _clock.Now;

        ActivePause.EndedAt = resumedAt;

        ActiveSession = new WorkSession
        {
            ProjectId = ActiveProjectId.Value,
            StartedAt = resumedAt
        };

        _dbContext.WorkSessions.Add(ActiveSession);

        await _dbContext.SaveChangesAsync();

        ActivePause = null;
        State = WorkTimerState.Running;
    }

    public async Task FinishAsync()
    {
        if (State == WorkTimerState.Stopped)
        {
            return;
        }

        var finishedAt = _clock.Now;

        if (State == WorkTimerState.Running)
        {
            ActiveSession!.EndedAt = finishedAt;
            ActiveSession = null;
        }
        else if (State == WorkTimerState.Paused)
        {
            if (ActivePause is null)
            {
                throw new InvalidOperationException(
                    "Paused state has no active pause.");
            }

            ActivePause.EndedAt = finishedAt;
            ActivePause = null;
        }

        await _dbContext.SaveChangesAsync();

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
        if (State != WorkTimerState.Running ||
            ActiveSession is null ||
            ActiveProjectId is null)
        {
            throw new InvalidOperationException(
                "The timer is not currently running.");
        }
    }

    private async Task EnsureProjectCanBeUsedAsync(
        int projectId)
    {
        var exists = await _dbContext.Projects
            .AnyAsync(x =>
                x.Id == projectId &&
                !x.IsArchived);

        if (!exists)
        {
            throw new InvalidOperationException(
                "The selected project does not exist or is archived.");
        }
    }

    private async Task EnsureNothingActiveAsync()
    {
        var hasWorkSession =
            await _dbContext.WorkSessions
                .AnyAsync(x => x.EndedAt == null);

        var hasPause =
            await _dbContext.PauseIntervals
                .AnyAsync(x => x.EndedAt == null);

        if (hasWorkSession || hasPause)
        {
            throw new InvalidOperationException(
                "An active timer state already exists.");
        }
    }
}