namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface ICardboardTranslationsSourceService : ISourceVolumeService { }

public class CardboardTranslationsSourceService(
    IFlareSolver _flare,
    IApiService _api,
    ISmartReaderService _smart,
    ILogger<CardboardTranslationsSourceService> _logger) 
    : FlareVolumeSource(_flare, _smart, _logger), ICardboardTranslationsSourceService
{
    public override string Name => "cardboard-translations";

    public override string RootUrl => "https://www.cardboardtranslation.com";
    public override int MaxRequestsBeforePauseMin => 0;
    public override int MaxRequestsBeforePauseMax => 0;
    public override int PauseDurationSecondsMin => 0;
    public override int PauseDurationSecondsMax => 0;

    public async IAsyncEnumerable<Entry> AllEntries(string title, int start = 1, int max = 150)
    {
        while(true)
        {
#if DEBUG
            _logger.LogInformation("Fetching entries for {title} - start: {start}, max: {max}",
                title, start, max);
#endif

            var contents = await FetchContents(title, start, max);
            if (contents is null) yield break;

            foreach (var entry in contents.Feed.Entries)
                yield return entry;

            if (contents.Feed.Entries.Length == 0) 
                yield break;

            var current = contents.Feed.Entries.Length;
            if (contents.Feed.StartIndex.Value + current > contents.Feed.TotalResults.Value)
                yield break;
            start += current;
        }
    }

    public async Task<PostContent?> FetchContents(string title, int start = 1, int max = 150)
    {
        var encoded = WebUtility.UrlEncode(title.Replace("?", ""));
        var callback = "test";
        var url = $"{RootUrl}/feeds/posts/default/-/{encoded}?alt=json-in-script&start-index={start}&max-results={max}&callback={callback}";
        var response = await _api.Create(url).Result();
        if (response is null)
        {
            _logger.LogError("Failed to fetch contents from {url}", url);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to fetch contents from {url} - {status}", url, response.StatusCode);
            return null;
        }

        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogError("Failed to fetch contents from {url} - Empty content", url);
            return null;
        }

        var splitter = callback + "(";
        var cleaned = string.Join(splitter, content
            .Split(splitter)
            .Skip(1))
            .Trim()
            .TrimEnd(';')
            .TrimEnd(')');

        var post = JsonSerializer.Deserialize<PostContent>(cleaned);
        if (post is null)
        {
            _logger.LogError("Failed to deserialize contents from {url} >> {cleaned}", url, cleaned);
            return null;
        }

        return post;
    }

    /* 
     * DETERMINE LABEL FROM CHAPTER:
     * search for `ch_SELECT.run(<label>, <url>, 'Select Chapter');`
     * use label to run the `FetchContents` method
     * 
     * |]========> <========[|
     * 
     * DETERMINE LABEL FROM ROOT:
     * search for `var label_chapter = '<label>';`
     * use label to run the `FetchContents` method
     */

    public override Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        throw new NotImplementedException();
    }

    public override string? NextUrl(HtmlDocument doc, string url)
    {
        throw new NotImplementedException();
    }

    public override IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        throw new NotImplementedException();
    }

    public override string SeriesFromChapter(string url)
    {
        throw new NotImplementedException();
    }

    public record class PostContent(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("encoding")] string Encoding,
        [property: JsonPropertyName("feed")] Feed Feed);

    public record class TextValue(
        [property: JsonPropertyName("$t")] string Text,
        [property: JsonPropertyName("type")] string? Type = null);

    public record class DateValue(
        [property: JsonPropertyName("$t")] DateTime Date);

    public record class Category(
        [property: JsonPropertyName("term")] string Term,
        [property: JsonPropertyName("scheme")] string? Scheme = null);

    public record class Link(
        [property: JsonPropertyName("rel")] string Rel,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("href")] string Href,
        [property: JsonPropertyName("title")] string? Title = null);

    public record class IntValue(
        [property: JsonPropertyName("$t")] string StrValue)
    {
        [JsonIgnore]
        public int Value => int.TryParse(StrValue, out var value) ? value : 0;
    }

    public record class Entry(
        [property: JsonPropertyName("id")] TextValue Id,
        [property: JsonPropertyName("updated")] DateValue Updated,
        [property: JsonPropertyName("published")] DateValue Published,
        [property: JsonPropertyName("category")] Category[] Categories,
        [property: JsonPropertyName("title")] TextValue Title,
        [property: JsonPropertyName("content")] TextValue Content,
        [property: JsonPropertyName("link")] Link[] Links);

    public record class Feed(
        [property: JsonPropertyName("id")] TextValue Id,
        [property: JsonPropertyName("updated")] DateValue Updated,
        [property: JsonPropertyName("category")] Category[] Categories,
        [property: JsonPropertyName("title")] TextValue Title,
        [property: JsonPropertyName("subtitle")] TextValue SubTitle,
        [property: JsonPropertyName("link")] Link[] Links,
        [property: JsonPropertyName("openSearch$totalResults")] IntValue TotalResults,
        [property: JsonPropertyName("openSearch$startIndex")] IntValue StartIndex,
        [property: JsonPropertyName("openSearch$itemsPerPage")] IntValue ItemsPerPage,
        [property: JsonPropertyName("entry")] Entry[] Entries);
}
