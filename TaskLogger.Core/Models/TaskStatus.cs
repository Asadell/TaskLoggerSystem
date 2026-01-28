namespace TaskLogger.Core.Models;

public enum TaskState
{
    Pending,
    Running,
    Completed,
    Failed
}

public class TaskStatus
{
    public string TaskId { get; set; } = string.Empty;
    public TaskState State { get; set; }
    public string? WorkerId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}