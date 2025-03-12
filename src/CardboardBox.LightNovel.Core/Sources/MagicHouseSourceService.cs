
namespace CardboardBox.LightNovel.Core.Sources;

public interface IMagicHouseSourceService : ISourceVolumeService { }

internal class MagicHouseSourceService(
    IApiService _api,
    ILogger<MagicHouseSourceService> _logger) : RatedSource, IMagicHouseSourceService
{
    public string Name => "magic-house";

    public string RootUrl => "https://magichousetldotcom.wordpress.com";

    private readonly Dictionary<string, SourceVolume[]> _volumes = [];

    public SourceVolume[] GetVolumes(string url, HtmlDocument doc)
    {
        url = url.Trim('/').ToLower();

        if (_volumes.TryGetValue(url, out var volumes))
            return volumes;

        return _volumes[url] = ParseVolumes(doc, url).ToArray();
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        string rootUrl = firstUrl.GetRootUrl(),
               url = firstUrl;

        var limiter = CreateLimiter(() =>
        {
            var currentUrl = url;
            return GetChapter(currentUrl, rootUrl);
        });

        using var tsc = new CancellationTokenSource();
        await foreach (var chap in limiter.Fetch(_logger, tsc.Token))
        {
            if (chap is null)
            {
                tsc.Cancel();
                break;
            }

            yield return chap;

            if (string.IsNullOrEmpty(chap.NextUrl))
            {
                tsc.Cancel();
                break;
            }

            url = chap.NextUrl;
        }
    }

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get chapter: {url}", url);
            return null;
        }

        var reader = new SmartReader.Reader(url, doc.Text)
        {
            Debug = true,
            LoggerDelegate = (msg) => _logger.LogDebug("[SMART READER] {url} >> {msg}", url, msg)
        };

        var article = await reader.GetArticleAsync();
        if (article is null || !article.Completed || !article.IsReadable)
        {
            var errors = article?.Errors?.ToArray() ?? [];
            foreach (var error in errors)
                _logger.LogError(error, "[SMART READER] Failed to read >> {url}", url);
            _logger.LogWarning("Could not get article for {url}", url);
            return null;
        }

        var title = article.Title;
        var content = article.Content;

        var nextUrl = doc.DocumentNode
            .SelectNodes("//div[@class='wp-block-buttons is-layout-flex wp-block-buttons-is-layout-flex']" +
                "/div[@class='wp-block-button']/a[@class='wp-block-button__link wp-element-button']")
            .FirstOrDefault(t => t is not null && t.InnerText.ToLower().Trim().Contains("next"))?
            .GetAttributeValue("href", string.Empty);

        return new SourceChapter(bookTitle, title, content, nextUrl ?? string.Empty, url);
    }

    public IEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        var links = doc.DocumentNode.SelectNodes("//div[@class='wp-block-buttons is-layout-flex wp-block-buttons-is-layout-flex']/div[@class='wp-block-button']/a");
        if (links is null)
        {
            _logger.LogError("Failed to get volumes");
            yield break;
        }

        var chapters = links
            .Select(t =>
            {
                var link = t.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrEmpty(link)) return null;

                var title = t.InnerText?.HTMLDecode()?.Trim() ?? string.Empty;
                return new SourceChapterItem
                {
                    Title = title,
                    Url = link
                };
            })
            .Where(t => t is not null)
            .Select(t => t!)
            .ToArray();
        if (chapters.Length == 0) yield break;

        var images = doc.DocumentNode
            .SelectNodes("//figure[@class='wp-block-image size-large']/img")?
            .Select(t => t.GetAttributeValue("data-orig-file", string.Empty))
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!)
            .ToArray() ?? [];

        yield return new SourceVolume
        {
            Title = "Volume 1",
            Url = url,
            Chapters = chapters,
            Inserts = images,
            Forwards = images
        };
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return null;
        }

        var title = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", string.Empty)?.HTMLDecode();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogError("Failed to get series title: {url}", url);
            return null;
        }

        var description = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", string.Empty)?.HTMLDecode();
        var authors = Array.Empty<string>();
        var genres = Array.Empty<string>();
        var tags = Array.Empty<string>();
        var image = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", string.Empty);

        var volumes = GetVolumes(url, doc);
        var firstChap = volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url;
        return new TempSeriesInfo(title, description, authors, image, firstChap, genres, tags);
    }

    public string SeriesFromChapter(string url)
    {
        var part = url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrEmpty(part)) throw new NullReferenceException("Could not find series from chapter");

        var parts = string.Join('-', url.Split('-').Skip(1));
        return $"{RootUrl}/{parts}/";
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string url)
    {
        var safeSeriesUrl = url.Trim('/').ToLower();
        if (_volumes.TryGetValue(safeSeriesUrl, out var volumes))
        {
            foreach (var volume in volumes)
                yield return volume;
            yield break;
        }

        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            yield break;
        }

        volumes = GetVolumes(url, doc);
        foreach (var volume in volumes)
            yield return volume;
    }
}
