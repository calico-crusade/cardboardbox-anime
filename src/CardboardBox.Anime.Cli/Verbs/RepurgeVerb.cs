using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

using LightNovel.Core;
using LightNovel.Core.Sources.Utilities;

[Verb("repurge", HelpText = "Re-purge the content of all chapters for a given series")]
public class RepurgeOptions
{
    [Option('i', "id", HelpText = "The ID of the series to repurge", Required = true)]
    public long SeriesId { get; set; }
}

internal class RepurgeVerb(
    ILnDbService _db,
    ISmartReaderService _smart,
    ILogger<RepurgeVerb> logger) : BooleanVerb<RepurgeOptions>(logger)
{
    public override async Task<bool> Execute(RepurgeOptions options, CancellationToken token)
    {
        const int LOG_AFTER = 10;

        var series = await _db.Series.Scaffold(options.SeriesId);
        if (series == null)
        {
            _logger.LogWarning("Couldn't find series with id {id}", options.SeriesId);
            return false;
        }

        var pages = series.Books
            .SelectMany(t => t.Chapters)
            .SelectMany(t => t.Pages)
            .Select(t => t.Page)
            .ToArray();

        int changeCount = 0;
        int count = 0;
        foreach(var page in pages)
        {
            count++;
            var changed = _smart.CleanseHtml(page.Content, series.Series.Url.GetRootUrl());
            if (changed == page.Content) continue;

            changeCount++;
            page.Content = changed;
            await _db.Pages.Update(page);

            if (changeCount % LOG_AFTER == 0)
                _logger.LogInformation("Re-purged {count} pages so far for series {seriesId}. {remaining} pages remaining.", 
                    changeCount, options.SeriesId, pages.Length - count);
        }

        _logger.LogInformation("Re-purged {count} pages for series {seriesId}", changeCount, options.SeriesId);
        return true;
    }
}
