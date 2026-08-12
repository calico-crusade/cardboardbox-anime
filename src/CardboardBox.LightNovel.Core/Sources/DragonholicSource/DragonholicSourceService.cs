namespace CardboardBox.LightNovel.Core.Sources.DragonholicSource;

public interface IDragonholicSourceService : ISourceVolumeService { }

public class DragonholicSourceService(
    IDragonholicApiService _api,
    ILogger<DragonholicSourceService> _logger) : IDragonholicSourceService
{
    public string Name => "dragonholic";

    public string RootUrl => DragonholicApiService.RootUrl;

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var slug = SeriesSlug(url);
        var series = await _api.Series(slug);
        if (series is null)
        {
            _logger.LogError("Failed to get Dragonholic series: {url}", url);
            return null;
        }

        var chapters = await _api.Chapters(series.Id);
        var terms = series.Embedded?.Terms.SelectMany(term => term) ?? [];
        var authors = Terms(terms, "series-author");
        var genres = Terms(terms, "genre");
        var tags = Terms(terms, "series-tag");
        var image = series.Embedded?.FeaturedMedia
            .Select(media => media.SourceUrl)
            .FirstOrDefault(source => !string.IsNullOrWhiteSpace(source));
        var first = chapters.FirstOrDefault();
        var firstChapter = first is null ? null : ChapterUrl(series, first);

        return new TempSeriesInfo(
            Decode(series.Title.Rendered),
            series.Content.Rendered,
            authors,
            image,
            firstChapter,
            genres,
            tags);
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
    {
        var slug = SeriesSlug(seriesUrl);
        var series = await _api.Series(slug);
        if (series is null)
        {
            _logger.LogError("Failed to get Dragonholic series: {url}", seriesUrl);
            yield break;
        }

        var chapters = await _api.Chapters(series.Id);
        if (chapters.Length == 0)
            yield break;

        var images = series.Embedded?.FeaturedMedia
            .Select(media => media.SourceUrl)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .ToArray() ?? [];

        foreach (var group in VolumeGroups(chapters))
        {
            yield return new SourceVolume
            {
                Title = VolumeTitle(group.Key),
                Url = series.Link,
                Chapters = group
                    .Select(chapter => new SourceChapterItem
                    {
                        Title = ChapterTitle(chapter),
                        Url = ChapterUrl(series, chapter)
                    })
                    .ToArray(),
                Forwards = images,
                Inserts = images
            };
        }
    }

    private static IEnumerable<IGrouping<DragonholicVolume, DragonholicChapterItem>> VolumeGroups(
        DragonholicChapterItem[] chapters)
    {
        return chapters
            .GroupBy(chapter => new DragonholicVolume(
                chapter.VolumeId,
                chapter.VolumeName,
                chapter.VolumeOrder))
            .OrderBy(group => group.Key.Order ?? int.MaxValue)
            .ThenBy(group => group.Min(chapter => chapter.ChapterOrder));
    }

    private static string VolumeTitle(DragonholicVolume volume)
    {
        if (!string.IsNullOrWhiteSpace(volume.Name))
            return Decode(volume.Name);

        return volume.Order.HasValue
            ? $"Volume {volume.Order.Value}"
            : "Volume 1";
    }

    private static string ChapterTitle(DragonholicChapterItem chapter)
    {
        var heading = string.IsNullOrWhiteSpace(chapter.Heading)
            ? chapter.Name
            : chapter.Heading;
        var title = Decode(heading);
        var subtitle = Decode(chapter.Subtitle ?? string.Empty);

        return string.IsNullOrWhiteSpace(subtitle)
            ? title
            : $"{title}: {subtitle}";
    }

    private static string ChapterUrl(DragonholicSeries series, DragonholicChapterItem chapter)
    {
        return $"{series.Link.TrimEnd('/')}/{Uri.EscapeDataString(chapter.Slug)}/";
    }

    private sealed record DragonholicVolume(string? Id, string? Name, int? Order);

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        var (series, chapter, chapters) = await FindChapter(url);
        if (series is null || chapter is null)
        {
            _logger.LogError("Failed to find Dragonholic chapter: {url}", url);
            return null;
        }

        var content = await _api.Chapter(chapter.Id);
        if (content is null)
        {
            _logger.LogError("Failed to get Dragonholic chapter: {url}", url);
            return null;
        }

        var index = Array.FindIndex(chapters, item => item.Id == chapter.Id);
        var nextUrl = index >= 0 && index + 1 < chapters.Length
            ? ChapterUrl(series, chapters[index + 1])
            : string.Empty;

        return new SourceChapter(
            bookTitle,
            ChapterTitle(chapter),
            content.Content.Rendered,
            nextUrl,
            ChapterUrl(series, chapter));
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        var (series, firstChapter, chapters) = await FindChapter(firstUrl);
        if (series is null || firstChapter is null)
            yield break;

        var start = Array.FindIndex(chapters, chapter => chapter.Id == firstChapter.Id);
        if (start < 0)
            yield break;

        for (var index = start; index < chapters.Length; index++)
        {
            var chapter = chapters[index];
            var bookTitle = VolumeTitle(new(
                chapter.VolumeId,
                chapter.VolumeName,
                chapter.VolumeOrder));
            var loaded = await GetChapter(ChapterUrl(series, chapter), bookTitle);
            if (loaded is not null)
                yield return loaded;
        }
    }

    public string SeriesFromChapter(string url)
    {
        return $"{RootUrl}/series/{SeriesSlug(url)}/";
    }

    private async Task<(
        DragonholicSeries? series,
        DragonholicChapterItem? chapter,
        DragonholicChapterItem[] chapters)> FindChapter(string url)
    {
        var series = await _api.Series(SeriesSlug(url));
        if (series is null)
            return (null, null, []);

        var chapters = await _api.Chapters(series.Id);
        var safeUrl = NormalizeUrl(url);
        var chapterSlug = PathParts(url).Skip(2).FirstOrDefault();
        var chapter = chapters.FirstOrDefault(item =>
            NormalizeUrl(ChapterUrl(series, item)) == safeUrl);

        if (chapter is null && !string.IsNullOrWhiteSpace(chapterSlug))
            chapter = chapters.FirstOrDefault(item =>
                string.Equals(item.Slug, chapterSlug, StringComparison.OrdinalIgnoreCase));

        return (series, chapter, chapters);
    }

    private static string SeriesSlug(string url)
    {
        var parts = PathParts(url);
        if (parts.Length >= 2 &&
            string.Equals(parts[0], "series", StringComparison.OrdinalIgnoreCase))
            return parts[1];

        throw new ArgumentException($"Could not determine Dragonholic series from URL: {url}", nameof(url));
    }

    private static string[] PathParts(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return [];

        return uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
    }

    private static string NormalizeUrl(string url) => url.Trim().TrimEnd('/');

    private static string Decode(string value)
    {
        return WebUtility.HtmlDecode(value)
            .Replace("\\\"", "\"")
            .Replace("\\'", "'")
            .Trim();
    }

    private static string[] Terms(IEnumerable<DragonholicTerm> terms, string taxonomy)
    {
        return terms
            .Where(term => string.Equals(term.Taxonomy, taxonomy, StringComparison.OrdinalIgnoreCase))
            .Select(term => Decode(term.Name))
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
