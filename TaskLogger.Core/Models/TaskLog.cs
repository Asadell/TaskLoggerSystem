namespace TaskLogger.Core.Models;

public class TaskLog
{
    public string TaskId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "INFO"; // INFO, WARN, ERROR
    public string Message { get; set; } = string.Empty;
}