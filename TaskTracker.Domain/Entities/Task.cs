using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

public class Task
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public Status TaskStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}