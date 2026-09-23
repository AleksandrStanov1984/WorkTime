using System.IO;
using Microsoft.EntityFrameworkCore;
using WorkTime.Models;

namespace WorkTime.Data;

public sealed class WorkTimeDbContext : DbContext
{
    public WorkTimeDbContext()
    {
    }

    public WorkTimeDbContext(
        DbContextOptions<WorkTimeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkSession> WorkSessions => Set<WorkSession>();

    public DbSet<PauseInterval> PauseIntervals => Set<PauseInterval>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "WorkTime");

        Directory.CreateDirectory(appDataPath);

        var databasePath =
            Path.Combine(appDataPath, "worktime.db");

        optionsBuilder.UseSqlite(
            $"Data Source={databasePath}");
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(x => x.HourlyRate)
                .HasPrecision(10, 2);

            entity.Property(x => x.AgreedPrice)
                .HasPrecision(12, 2);
        });

        modelBuilder.Entity<WorkSession>(entity =>
        {
            entity.HasOne(x => x.Project)
                .WithMany(x => x.WorkSessions)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PauseInterval>(entity =>
        {
            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}