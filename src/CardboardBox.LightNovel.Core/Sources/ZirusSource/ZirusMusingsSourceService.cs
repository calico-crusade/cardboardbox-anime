namespace CardboardBox.LightNovel.Core.Sources.ZirusSource;

public interface IZirusMusingsSourceService : ISourceVolumeService { }

public class ZirusMusingsSourceService : IZirusMusingsSourceService
{
    private readonly IZirusApiService _api;
    private readonly IConfiguration _config;
    private readonly IMarkdownService _markdown;

    public string RootUrl => "https://zirusmusings.net";

    public string Name => "zirus";

    public string SeriesKey => _config["NovelSources:ZirusKey"] ?? throw new ArgumentNullException("Zirus Key not found in config");

    public string ApiUrl => $"{RootUrl}/_next/data/{SeriesKey}/";

    public ZirusMusingsSourceService(
        IZirusApiService api,
        IConfiguration config,
        IMarkdownService markdown)
    {
        _api = api;
        _config = config;
        _markdown = markdown;
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        var seriesId = SeriesFromChapter(firstUrl);

        var series = await _api.Series(seriesId);
        if (series == null) yield break;

        var data = series.PageProps.Data;

        foreach(var volume in data.Volumes)
        {
            if (volume.Translated == 0) continue;

            foreach(var chap in volume.Chapters)
            {
                if (string.IsNullOrEmpty(chap.Translator) ||
                    string.IsNullOrEmpty(chap.Published))
                    continue;

                var url = _api.BuildChapUrl(seriesId, volume.Number, chap.Chapter);
                var chapter = await GetChapter(url, volume.Title);
                if (chapter == null) continue;

                yield return chapter;
            }
        }
    }

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        var chapter = await _api.Chapter(url);
        if (chapter == null) return null;

        string? nextUrl = null;
        if (chapter.PageProps.Data.Next != null)
            nextUrl = _api.BuildChapUrl(
                chapter.PageProps.Data.Series,
                chapter.PageProps.Data.Next);

        var html = HandleImages(_markdown.ToHtml(chapter.PageProps.Content));

        return new SourceChapter(
            bookTitle,
            chapter.PageProps.Data.ToString(),
            html,
            nextUrl ?? string.Empty,
            url);
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var seriesId = url.ToLower().StartsWith("http")
            ? url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last()
            : url;

        var series = await _api.Series(seriesId);
        if (series == null) return null;

        var item = series.PageProps.Data;
        if (item == null) return null;

        var first = item.Volumes.First().Chapters.First();

        var firstChap = _api.BuildChapUrl(seriesId, first.Volume, first.Chapter);

        var cover = item.Cover;
        if (!string.IsNullOrEmpty(cover))
            cover = $"{RootUrl}{cover}";

        return new TempSeriesInfo(
            item.Name,
            _markdown.ToHtml(item.Summary),
            new[] { item.Author },
            cover,
            firstChap,
            Array.Empty<string>(),
            Array.Empty<string>());
    }

    public string SeriesFromChapter(string url)
    {
        if (!url.ToLower().StartsWith("http"))
            return url;

        if (!url.ToLower().Contains("seriesId="))
            return url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

        var parts = url.Split(new[] { "seriesId=" }, StringSplitOptions.RemoveEmptyEntries).Last();
        return parts.Split('&', StringSplitOptions.RemoveEmptyEntries).First();
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
    {
        var seriesId = SeriesFromChapter(seriesUrl);
        var series = await _api.Series(seriesId);
        if (series == null) yield break;

        var data = series.PageProps.Data;

        var inserts = string.IsNullOrEmpty(data.Cover)
            ? Array.Empty<string>()
            : new[] { $"{RootUrl}{data.Cover}" };

        foreach (var volume in data.Volumes)
        {
            if (volume.Translated == 0) continue;

            var chapters = volume.Chapters
                .Where(t =>
                    !string.IsNullOrEmpty(t.Translator) &&
                    !string.IsNullOrEmpty(t.Published))
                .Select(t => new SourceChapterItem
                {
                    Title = t.ToString(),
                    Url = _api.BuildChapUrl(seriesId, volume.Number, t.Chapter)
                })
                .ToArray();

            if (chapters.Length == 0) continue;

            var imgs = string.IsNullOrEmpty(volume.Cover)
                ? Array.Empty<string>()
                : new[] { $"{RootUrl}{volume.Cover}" };

            yield return new SourceVolume
            {
                Title = volume.Title,
                Forwards = imgs,
                Inserts = inserts,
                Url = seriesUrl,
                Chapters = chapters
            };
        }
    }

    public string HandleImages(string html)
    {
        if (!html.ToLower().Contains("<img"))
            return html;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        doc
            .DocumentNode
            .SelectNodes("//img")
            .Each(t =>
            {
                var src = t.GetAttributeValue("src", string.Empty);
                if (string.IsNullOrEmpty(src)) return;

                if (src.StartsWith("/"))
                    t.SetAttributeValue("src", $"{RootUrl}{src}");
            });

        doc
            .DocumentNode
            .ChildNodes
            .ToArray()
            .Where(t => t.InnerHtml.Contains("<img"))
            .Each(t => t.CleanupNode());

        return doc.DocumentNode.InnerHtml;
    }
}