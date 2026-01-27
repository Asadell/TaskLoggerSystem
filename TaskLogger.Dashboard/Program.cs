using System.Text;
using System.Text.Json;
using NATS.Client;
using NATS.Client.JetStream;
using Spectre.Console;
using Spectre.Console.Rendering;
using TaskLogger.Core.Models;
using TaskLogger.Infrastructure.Messaging;
using TaskStatus = TaskLogger.Core.Models.TaskStatus;

AnsiConsole.Write(
    new FigletText("Task Dashboard")
        .LeftJustified()
        .Color(Color.Yellow));

using var nats = new NatsConnection();

var taskStatuses = new Dictionary<string, TaskStatus>();
var recentLogs = new List<TaskLog>();
const int maxLogs = 20;

// Subscribe to status updates
nats.Connection.SubscribeAsync("status.>", (sender, args) =>
{
    var json = Encoding.UTF8.GetString(args.Message.Data);
    var status = JsonSerializer.Deserialize<TaskStatus>(json);
    
    if (status != null)
    {
        taskStatuses[status.TaskId] = status;
    }
});

// Subscribe to logs
var logOptions = PushSubscribeOptions.Builder()
    .WithStream("LOGS")
    .Build();

nats.JetStream.PushSubscribeAsync("logs.>", (sender, args) =>
{
    var json = Encoding.UTF8.GetString(args.Message.Data);
    var log = JsonSerializer.Deserialize<TaskLog>(json);
    
    if (log != null)
    {
        recentLogs.Add(log);
        if (recentLogs.Count > maxLogs)
        {
            recentLogs.RemoveAt(0);
        }
    }
}, false, logOptions);

// Real-time dashboard
await AnsiConsole.Live(new Panel("Initializing..."))
    .StartAsync(async ctx =>
    {
        while (true)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Task ID")
                .AddColumn("State")
                .AddColumn("Worker")
                .AddColumn("Duration");

            foreach (var (taskId, status) in taskStatuses.OrderByDescending(x => x.Value.StartedAt))
            {
                var duration = status.CompletedAt.HasValue && status.StartedAt.HasValue
                    ? (status.CompletedAt.Value - status.StartedAt.Value).TotalSeconds.ToString("F1") + "s"
                    : "N/A";

                var stateColor = status.State switch
                {
                    TaskState.Running => "yellow",
                    TaskState.Completed => "green",
                    TaskState.Failed => "red",
                    _ => "grey"
                };

                table.AddRow(
                    taskId[..8],
                    $"[{stateColor}]{status.State}[/]",
                    status.WorkerId?[..15] ?? "N/A",
                    duration
                );
            }

            var logPanel = new Panel(
                string.Join("\n", recentLogs.TakeLast(10).Select(l => 
                    $"[grey]{l.Timestamp:HH:mm:ss}[/] [{GetLogColor(l.Level)}]{l.Level}[/] {l.Message}"
                ))
            ).Header("Recent Logs");

            var layout = new Layout()
                .SplitRows(
                    new Layout("Top").Size(15),
                    new Layout("Bottom")
                );

            layout["Top"].Update(table);
            layout["Bottom"].Update(logPanel);

            ctx.UpdateTarget(layout);
            
            await Task.Delay(500);
        }
    });

static string GetLogColor(string level) => level switch
{
    "ERROR" => "red",
    "WARN" => "yellow",
    _ => "blue"
};