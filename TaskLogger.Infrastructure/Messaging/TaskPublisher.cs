using System.Text;
using System.Text.Json;
using NATS.Client;
using NATS.Client.JetStream;
using TaskLogger.Core.Models;

namespace TaskLogger.Infrastructure.Messaging;

public interface ITaskPublisher
{
    Task<string> PublishTaskAsync(TaskPayload task);
    Task PublishLogAsync(TaskLog log);
    Task PublishStatusAsync(TaskStatus status);
}

public class TaskPublisher : ITaskPublisher
{
    private readonly INatsConnection _nats;

    public TaskPublisher(INatsConnection nats)
    {
        _nats = nats;
    }

    public async Task<string> PublishTaskAsync(TaskPayload task)
    {
        var json = JsonSerializer.Serialize(task);
        var data = Encoding.UTF8.GetBytes(json);
        
        var pubAck = await _nats.JetStream.PublishAsync("tasks.pending", data);
        
        Console.WriteLine($"Task published: {task.Id} (seq: {pubAck.Seq})");
        return task.Id;
    }

    public async Task PublishLogAsync(TaskLog log)
    {
        var json = JsonSerializer.Serialize(log);
        var data = Encoding.UTF8.GetBytes(json);
        
        await _nats.JetStream.PublishAsync($"logs.{log.TaskId}", data);
    }

    public async Task PublishStatusAsync(TaskStatus status)
    {
        var json = JsonSerializer.Serialize(status);
        var data = Encoding.UTF8.GetBytes(json);
        
        _nats.Connection.Publish($"status.{status.TaskId}", data);
    }
}