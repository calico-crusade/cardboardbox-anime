using CardboardBox.LightNovel.Core;
using CommandLine;
using Spectre.Console;

namespace CardboardBox.Anime.Cli.Verbs;

using CardboardBox.Anime.Core.Models;
using CardboardBox.Anime.Database;
using CardboardBox.Extensions;
using CardboardBox.Manga;
using Interactive;
using System.Collections.Generic;
using System.IO;

[Verb("interactive", true, HelpText = "Starts an interactive session for doing stuff")]
public class InteractiveOptions { }

internal class InteractiveVerb(
    ILogger<InteractiveVerb> logger,
    ILnDbService _novels,
    IMangaDbService _mangaDb,
    IMangaService _manga,
    INovelApiService _novelApi,
    IApiService _api,
    INovelEpubService _epub) : BooleanVerb<InteractiveOptions>(logger)
{
    public Series[] Series { get; set; } = [];

    public async Task<bool> ReloadSeries(Series series)
    {
        _logger.LogInformation("Reloading series {series}", series.Title);
        var count = await _novelApi.Load(series);
        if (count == -1)
        {
            _logger.LogWarning("Couldn't find existing series count with that id!");
            return false;
        }

        if (count == 0)
        {
            _logger.LogWarning("No new chapters found for that series!");
            return true;
        }

        _logger.LogInformation("Loaded {count} new chapters for series {series}", count, series.Title);
        return true;
    }

    public async Task<bool> GenerateEPUBs(Series series)
    {
        var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "novels");
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
            _logger.LogInformation("Created output directory {output}", output);
        }

        _logger.LogInformation("Generating EPUB for series {series}", series.Title);

        var books = await _novels.Books.BySeries(series.Id);
        if (books.Length == 0)
        {
            _logger.LogWarning("No books found for series {series}", series.Title);
            return false;
        }

        var res = await _epub.Generate(books.Select(t => t.Id).ToArray());
        if (res is null)
        {
            _logger.LogWarning("An error occurred while generating epubs.");
            return false;
        }

        var (stream, name, type) = res;
        var actualName = name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? series.Title.PurgePathChars() + ".zip"
            : name;
        var path = Path.Combine(output, actualName);

        if (File.Exists(path))
        {
            _logger.LogWarning("File {path} already exists, overwriting", path);
            File.Delete(path);
        }

        using var io = File.Create(path);
        await stream.CopyToAsync(io);
        stream.Close();

        _logger.LogInformation("Generated EPUB file(s) [{type}] {path}", type, path);
        return true;
    }

    public (string, Func<Series, Task<bool>>)[] SelectActions()
    {
        var dictionary = new Dictionary<string, Func<Series, Task<bool>>>()
        {
            ["Update Chapters"] = ReloadSeries,
            ["Generate EPUBs"] = GenerateEPUBs,
        };

        return dictionary
            .ToArray()
            .MultiSelect("Action", t => t.Key)
            .Select(t => (t.Key, t.Value))
            .ToArray();
    }

    public async Task<bool> TriggerSeriesSelection(CancellationToken token)
    {
        if (Series.Length == 0)
        { 
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Loading Series...", async ctx =>
                {
                    Series = [.. (await _novels.Series.All()).OrderByDescending(t => t.Id)];
                });
        }

        Console.Clear();
        var series = InteractiveExtensions.SelectSeries(Series);

        if (series.Length == 0)
        {
            _logger.LogWarning("No series selected");
            return false;
        }

        var actions = SelectActions();
        if (actions.Length == 0)
        {
            _logger.LogWarning("No actions selected");
            return false;
        }

        await series.ParallelProcess(1, token, actions);
        _logger.LogInformation("Finished running actions for series {series}", series.Length);
        await Task.Delay(1000, token);
        return true;
    }

    public async Task<bool> TriggerMangaSelection(CancellationToken token)
    {
        var prompt = AnsiConsole.Prompt(new TextPrompt<string>("Manga Title to search for?").AllowEmpty());
        if (string.IsNullOrWhiteSpace(prompt))
        {
            _logger.LogWarning("No title provided");
            return false;
        }

        var filter = new MangaFilter
        {
            Search = prompt,
            State = TouchedState.All,
            Size = 50,
            Page = 1,
        };

        var series = await _mangaDb.Search(filter, null, true);
        if (series.Results.Length == 0)
        {
            _logger.LogWarning("No manga found with that title");
            return false;
        }

        var selected = series.Results
            .MultiSelect("Manga", t => Markup.Escape(t.Manga.Title));
        if (selected.Length == 0)
        {
            _logger.LogWarning("No manga selected");
            return false;
        }

        var manga = selected.First();
        var chapter = manga.Chapter;

        if (chapter.Pages is null || chapter.Pages.Length == 0)
            chapter.Pages = await _manga.MangaPages(chapter, false);

        if (chapter.Pages is null || chapter.Pages.Length == 0)
        {
            _logger.LogWarning("No pages found for that manga");
            return false;
        }

        var index = 0;
        while(true)
        {
            Stream? stream = null;
            var imageUrl = chapter.Pages[index];
            Console.Clear();
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Loading Image: {imageUrl}", async ctx =>
                {
                    var (currentStream, size, file, type) = await _api.GetData(imageUrl);
                    stream = currentStream;
                });

            Console.Clear();

            if (stream is null)
            {
                _logger.LogWarning("No stream found for that image");
                return false;
            }

            var image = new CanvasImage(stream);
            AnsiConsole.Write(image);
            AnsiConsole.MarkupLine($"[green]Page {index + 1}/{chapter.Pages.Length}[/]");
            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    if (index > 0) index--;
                    break;
                case ConsoleKey.RightArrow:
                    if (index < chapter.Pages.Length - 1) index++;
                    break;
                case ConsoleKey.Escape:
                    stream.Close();
                    return true;
            }

            stream.Close();
        }
    }

    public async Task<bool> LoadNewSelection(CancellationToken token)
    {
        var url = AnsiConsole.Prompt(new TextPrompt<string>("Novel URL: ").AllowEmpty());
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("No URL provided");
            return false;
        }

        Console.Clear();

        InteractiveExtensions.RegisterLog();

        int finalCount = -1;
        bool finalIsNew = true;
        url = url.Trim();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Loading Novel: {url}", async ctx =>
            {
                var (count, isNew) = await _novelApi.Load(url);
                finalCount = count;
                finalIsNew = isNew;
            });

        if (finalCount == -1)
        {
            _logger.LogWarning("Couldn't find existing series count with that id!");
            InteractiveExtensions.UnregisterLog();
            return false;
        }

        if (finalCount == 0)
        {
            _logger.LogWarning("No new chapters found for that series!");
            InteractiveExtensions.UnregisterLog();
            return true;
        }

        _logger.LogInformation("Loaded {count} chapters for {new} series {url}", finalCount, finalIsNew ? "new" : "old", url);
        InteractiveExtensions.UnregisterLog();
        return true;
    }

    public async Task<bool> ActionChoices(CancellationToken token)
    {
        var dic = new Dictionary<string, Func<CancellationToken, Task<bool>>>()
        {
            ["CBA Novels"] = TriggerSeriesSelection,
            ["CBA Load New Novel"] = LoadNewSelection,
            ["CBA Manga"] = TriggerMangaSelection,
        };

        var actions = dic
            .ToArray()
            .MultiSelect("Action", t => t.Key)
            .Select(t => t.Value)
            .ToArray();

        if (actions.Length == 0)
        {
            _logger.LogWarning("No actions selected");
            return false;
        }

        try
        {
            foreach (var action in actions)
            {
                if (token.IsCancellationRequested) return false;
                if (!await action(token)) return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running actions");
            return false;
        }

        return true;
    }

    public override async Task<bool> Execute(InteractiveOptions options, CancellationToken token)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        Console.Clear();
        while (await ActionChoices(token)) ;

        return true;
    }
}
