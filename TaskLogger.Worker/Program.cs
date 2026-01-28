using Spectre.Console;
using TaskLogger.Infrastructure.Messaging;
using TaskLogger.Worker;

AnsiConsole.Write(
    new FigletText("Task Worker")
        .LeftJustified()
        .Color(Color.Blue));

try
{
    using var nats = new NatsConnection();
    var publisher = new TaskPublisher(nats);
    var worker = new TaskWorker(nats, publisher);

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    await worker.StartAsync(cts.Token);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message.EscapeMarkup()}[/]");
}