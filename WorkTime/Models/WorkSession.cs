namespace WorkTime.Models;

public sealed class WorkSession
{
    public long Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public Project Project { get; set; } = null!;
}
