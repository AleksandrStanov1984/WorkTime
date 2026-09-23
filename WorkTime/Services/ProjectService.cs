using Microsoft.EntityFrameworkCore;
using WorkTime.Data;
using WorkTime.Models;

namespace WorkTime.Services;

public sealed class ProjectService
{
    private readonly WorkTimeDbContext _dbContext;

    public ProjectService(WorkTimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Project>> GetActiveProjectsAsync()
    {
        return await _dbContext.Projects
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<Project> CreateAsync(
        string name,
        decimal hourlyRate)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Project name is required.",
                nameof(name));
        }

        if (hourlyRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourlyRate),
                "Hourly rate cannot be negative.");
        }

        var project = new Project
        {
            Name = name,
            HourlyRate = hourlyRate,
            CreatedAt = DateTime.Now
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        return project;
    }

    public async Task ArchiveAsync(int projectId)
    {
        var project = await _dbContext.Projects
            .SingleOrDefaultAsync(x => x.Id == projectId);

        if (project is null)
        {
            throw new InvalidOperationException(
                "Project does not exist.");
        }

        project.IsArchived = true;

        await _dbContext.SaveChangesAsync();
    }

    public async Task<Project> UpdateAsync(
    int projectId,
    string name,
    decimal hourlyRate,
    decimal? agreedPrice,
    int? dailyTargetMinutes,
    int? reminderBeforeMinutes,
    bool notificationsEnabled)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Project name is required.",
                nameof(name));
        }

        if (hourlyRate < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hourlyRate),
                "Hourly rate cannot be negative.");
        }

        if (agreedPrice is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(agreedPrice),
                "Agreed price cannot be negative.");
        }

        if (dailyTargetMinutes is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dailyTargetMinutes),
                "Daily target must be greater than zero.");
        }

        if (reminderBeforeMinutes is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reminderBeforeMinutes),
                "Reminder cannot be negative.");
        }

        var project = await _dbContext.Projects
            .SingleOrDefaultAsync(x => x.Id == projectId);

        if (project is null)
        {
            throw new InvalidOperationException(
                "Project does not exist.");
        }

        project.Name = name;
        project.HourlyRate = hourlyRate;
        project.AgreedPrice = agreedPrice;
        project.DailyTargetMinutes = dailyTargetMinutes;
        project.ReminderBeforeMinutes = reminderBeforeMinutes;
        project.NotificationsEnabled = notificationsEnabled;

        await _dbContext.SaveChangesAsync();

        return project;
    }
}