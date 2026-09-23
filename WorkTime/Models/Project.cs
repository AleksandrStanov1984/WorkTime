namespace WorkTime.Models;

public sealed class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal? AgreedPrice { get; set; }
    public int? DailyTargetMinutes { get; set; }
    public int? ReminderBeforeMinutes { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<WorkSession> WorkSessions { get; set; } = [];
}
