using System.Text.Json;
using Spectre.Console;
using TaskLogger.Core.Models;
using TaskLogger.Infrastructure.Messaging;

AnsiConsole.Write(
    new FigletText("Task Publisher")
        .LeftJustified()
        .Color(Color.Green));

using var nats = new NatsConnection();
var publisher = new TaskPublisher(nats);

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .AddChoices("Submit Task", "Quick Submit (5 tasks)", "Exit"));

    if (choice == "Exit") break;

    if (choice == "Quick Submit (5 tasks)")
    {
        for (int i = 1; i <= 5; i++)
        {
            var task = new TaskPayload
            {
                Name = $"Quick Task {i}",
                Command = $"process-data-{i}",
                Priority = Random.Shared.Next(1, 11)
            };

            await publisher.PublishTaskAsync(task);
            AnsiConsole.MarkupLine($"[green]✓ Submitted: {task.Name}[/]");
        }
        continue;
    }

    var taskName = AnsiConsole.Ask<string>("Task [green]name[/]:");
    var command = AnsiConsole.Ask<string>("Task [blue]command[/]:");
    var priority = AnsiConsole.Ask<int>("Priority [yellow](1-10)[/]:");

    var newTask = new TaskPayload
    {
        Name = taskName,
        Command = command,
        Priority = priority
    };

    var taskId = await publisher.PublishTaskAsync(newTask);
    
    AnsiConsole.MarkupLine($"[green]✓ Task submitted with ID: {taskId}[/]");
    AnsiConsole.WriteLine();
}