using CardboardBox.LightNovel.Core;
using Spectre.Console;

namespace CardboardBox.Anime.Cli.Interactive;

public static class InteractiveExtensions
{
    public static string Trim(this string text, int maxLength, string replacer = "...")
    {
        if (text.Length > maxLength)
            return text[..(maxLength - replacer.Length)] + replacer;
        return text;
    }

    public static string FixTitle(Series series)
    {
        return FixTitle(series.Title);
    }

    public static string FixTitle(string text)
    {
        var maxLength = Console.WindowWidth - 20;
        return Markup.Escape(text.Trim(maxLength));
    }

    public static T[] MultiSelect<T>(this T[] items, string? name = null, Func<T, string>? display = null)
        where T : notnull
    {
        name = FixTitle(name ?? typeof(T).Name);
        display ??= t => t?.ToString() ?? string.Empty;

        var select = new MultiSelectionPrompt<T>()
            .Title($"Please select one or more {name} (order of selection matters)")
            .NotRequired()
            .PageSize(Console.WindowHeight - 5)
            .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
            .InstructionsText($"[grey](Press [blue]<space>[/] to toggle a {name}, [green]<enter>[/] to accept)[/]")
            .UseConverter(t => display(t))
            .AddChoices(items);
        return [.. AnsiConsole.Prompt(select)];
    }

    public static Series[] SelectSeries(Series[] series)
    {
        var maxId = series.Max(t => t.Id).ToString().Length;
        return series.MultiSelect(null, series => $"[grey]{series.Id.ToString().PadLeft(maxId)}[/] {FixTitle(series)}");
    }

    public static void PrintLogs(MessageData data)
    {
        var color = data.Level switch
        {
            "Verbose" => "grey",
            "Debug" => "blue",
            "Information" => "green",
            "Warning" => "yellow",
            "Error" => "red",
            "Fatal" => "red",
            _ => "white"
        };

        var level = data.Level switch
        {
            "Verbose" => "VER",
            "Debug" => "DBG",
            "Information" => "INF",
            "Warning" => "WRN",
            "Error" => "ERR",
            "Fatal" => "FTL",
            _ => data.Level[..3].ToUpper()
        };

        AnsiConsole.MarkupLine($"[{color}]{level}::{data.Timestamp:yyyy-MM-dd HH:mm:ss}[/] {Markup.Escape(data.Message)}");
    }

    public static void RegisterLog()
    {
        EventSink.OnLog += PrintLogs;
    }

    public static void UnregisterLog()
    {
        EventSink.OnLog -= PrintLogs;
    }

    public static Task ParallelProcess<T>(this IEnumerable<T> items,
        int parallels, CancellationToken token, 
        params (string title, Func<T, Task<bool>> action)[] actions)
    {
        async Task ProcessAction(T item, ParallelTask<T>[] tasks, int total)
        {
            var breakout = false;
            foreach(var task in tasks)
            {
                if (!breakout)
                { 
                    await task.Semaphore.WaitAsync(token);
                    breakout = !await task.Action(item);
                    task.Semaphore.Release();
                }

                task.Completed++;
                task.Task.Increment(1);

                if (task.Completed != total) continue;
                    
                task.Task.StopTask();
                task.Semaphore.Dispose();
            }
        }

        return AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                RegisterLog();

                var count = items.Count();
                var trackers = Enumerable.Range(0, actions.Length)
                    .Select(i => new ParallelTask<T>
                    {
                        Semaphore = new SemaphoreSlim(parallels),
                        Title = actions[i].title,
                        Action = actions[i].action,
                        Task = ctx.AddTask(actions[i].title, true, count),
                        Completed = 0
                    })
                    .ToArray();

                var tasks = new List<Task>();
                foreach (var item in items)
                {
                    tasks.Add(_ = Task.Run(() => ProcessAction(item, trackers, count), token));
                }

                await Task.WhenAll(tasks);

                foreach (var task in trackers)
                {
                    task.Task.StopTask();
                    task.Semaphore.Dispose();
                }

                UnregisterLog();
            });
    }

    internal class ParallelTask<T>
    {
        public required SemaphoreSlim Semaphore { get; init; }

        public required string Title { get; init; }

        public required Func<T, Task<bool>> Action { get; init; }

        public required ProgressTask Task { get; init; }

        public required int Completed { get; set; }
    }
}
