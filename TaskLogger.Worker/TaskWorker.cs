using System.Text;
using System.Text.Json;
using NATS.Client;
using NATS.Client.JetStream;
using Spectre.Console;
using TaskLogger.Core.Models;
using TaskLogger.Infrastructure.Messaging;

namespace TaskLogger.Worker;

public class TaskWorker
{
    private readonly INatsConnection _nats;
    private readonly ITaskPublisher _publisher;
    private readonly string _workerId;
    private CancellationTokenSource? _cts;

    public TaskWorker(INatsConnection nats, ITaskPublisher publisher)
    {
        _nats = nats;
        _publisher = publisher;
        _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid().ToString()[..8]}";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        AnsiConsole.MarkupLine($"[green]Worker {_workerId} started[/]");

        var options = PullSubscribeOptions.Builder()
            .WithDurable($"worker-{_workerId}")
            .Build();

        var subscription = _nats.JetStream.PullSubscribe("tasks.pending", options);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var messages = subscription.Fetch(5, 5000);

                foreach (var msg in messages)
                {
                    await ProcessTaskAsync(msg);
                }
            }
            catch (NATSTimeoutException)
            {
                // No messages available, continue
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            }

            await Task.Delay(100, _cts.Token);
        }
    }

    private async Task ProcessTaskAsync(Msg msg)
    {
        var json = Encoding.UTF8.GetString(msg.Data);
        var task = JsonSerializer.Deserialize<TaskPayload>(json);

        if (task == null)
        {
            msg.Nak();
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]Processing task: {task.Name} (ID: {task.Id})[/]");

        // Update status to Running
        await _publisher.PublishStatusAsync(new TaskStatus
        {
            TaskId = task.Id,
            State = TaskState.Running,
            WorkerId = _workerId,
            StartedAt = DateTime.UtcNow
        });

        try
        {
            // Simulate task execution
            await ExecuteTaskAsync(task);

            // Mark as completed
            await _publisher.PublishStatusAsync(new TaskStatus
            {
                TaskId = task.Id,
                State = TaskState.Completed,
                WorkerId = _workerId,
                CompletedAt = DateTime.UtcNow
            });

            msg.Ack();
            AnsiConsole.MarkupLine($"[green]✓ Task completed: {task.Name}[/]");
        }
        catch (Exception ex)
        {
            await _publisher.PublishStatusAsync(new TaskStatus
            {
                TaskId = task.Id,
                State = TaskState.Failed,
                WorkerId = _workerId,
                ErrorMessage = ex.Message,
                CompletedAt = DateTime.UtcNow
            });

            msg.Nak();
            AnsiConsole.MarkupLine($"[red]✗ Task failed: {task.Name} - {ex.Message}[/]");
        }
    }

    private async Task ExecuteTaskAsync(TaskPayload task)
    {
        await _publisher.PublishLogAsync(new TaskLog
        {
            TaskId = task.Id,
            Level = "INFO",
            Message = $"Starting task: {task.Name}"
        });

        // Simulate work with progress
        var steps = 5;
        for (int i = 1; i <= steps; i++)
        {
            await Task.Delay(1000);
            
            await _publisher.PublishLogAsync(new TaskLog
            {
                TaskId = task.Id,
                Level = "INFO",
                Message = $"Progress: {i}/{steps} - {task.Command}"
            });
        }

        await _publisher.PublishLogAsync(new TaskLog
        {
            TaskId = task.Id,
            Level = "INFO",
            Message = "Task completed successfully"
        });
    }
}