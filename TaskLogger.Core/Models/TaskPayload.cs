namespace TaskLogger.Core.Models;

public class TaskPayload
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public int Priority { get; set; } = 5; // 1-10
}