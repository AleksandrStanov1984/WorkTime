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
}