using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

using LightNovel.Core;

[Verb("load-novel", HelpText = "Inserts or updates a light novel series in the database")]
public class LoadNovelOptions
{
    //[Option('i', "id", HelpText = "The ID of the light novel series to update")]
    //public long? SeriesId { get; set; }

    //[Option('u', "url", HelpText = "The URL to the light novel series home page")]
    //public string? NovelUrl { get; set; }

    [Value(0, MetaName = "entries", HelpText = "The URL of the light novel series home page or ID of the series to load", Required = false)]
    public IEnumerable<string> Entries { get; set; } = [];
}

internal class LoadNovelVerb(
    ILogger<LoadNovelVerb> logger,
    INovelApiService _api,
    ILnDbService _db) : BooleanVerb<LoadNovelOptions>(logger)
{
    public async Task<bool> Load(string? url)
    {
        long? seriesId = null;
        if (long.TryParse(url, out var sid))
        {
            url = null;
            seriesId = sid;
        }

        if (string.IsNullOrEmpty(url) && seriesId == null)
        {
            _logger.LogWarning("Please specify the url or series ID");
            return false;
        }

        if (!string.IsNullOrEmpty(url) && seriesId != null)
        {
            _logger.LogWarning("Please specify only the url or only series ID, not both!");
            return false;
        }

        if (seriesId != null)
        {
            var series = await _db.Series.Fetch(seriesId ?? 0);
            if (series == null)
            {
                _logger.LogWarning("Couldn't find series with that id!");
                return false;
            }

            var count = await _api.Load(series);
            if (count == -1)
            {
                _logger.LogWarning("Couldn't find existing series count with that id!");
                return false;
            }
            if (count == 0)
            {
                _logger.LogWarning("No new chapters found for that series!");
                return false;
            }

            _logger.LogInformation("Loaded {count} new chapters for series {seriesId}", count, seriesId);
            return true;
        }

        var (newCount, isNew) = await _api.Load(url ?? "");
        if (newCount == -1)
        {
            _logger.LogWarning("Unable to load chapters from the given url!");
            return false;
        }

        _logger.LogInformation("Loaded {count} chapters for {new} series {url}", newCount, isNew ? "new" : "old", url);
        return true;
    }

    public async Task<bool> Load(IEnumerable<string> entires)
    {
        foreach(var entry in entires)
        {
            _logger.LogInformation("Loading {entry}", entry);
            try
            {
                var result = await Load(entry);
                _logger.LogInformation("Loaded {entry} - {result}", entry, result ? "Success" : "Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load {entry}", entry);
            }
        }

        return true;
    }

    public override Task<bool> Execute(LoadNovelOptions options, CancellationToken token)
    {
        _logger.LogInformation("Loading {count} entries: {series}", 
            options.Entries.Count(),
            string.Join("\r\n", options.Entries));
        return Load(options.Entries);
    }
}
