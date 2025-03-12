
namespace CardboardBox.LightNovel.Core.Sources;

public interface IVampiramtlSourceService : ISourceVolumeService { }

internal class VampiramtlSourceService(
    IApiService _api,
    ILogger<VampiramtlSourceService> _logger) : RatedSource, IVampiramtlSourceService
{
    public string Name => "vampiramtl";

    public string RootUrl => "https://www.vampiramtl.com";

    private readonly Dictionary<string, SourceVolume[]> _volumes = [];

    public SourceVolume[] GetVolumes(string url, HtmlDocument doc)
    {
        url = url.Trim('/').ToLower();

        if (_volumes.TryGetValue(url, out var volumes))
            return volumes;

        return _volumes[url] = ParseVolumes(doc, url).ToArray();
    }

    public IEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        var links = doc.DocumentNode.SelectNodes("//ul[@class='lcp_catlist']/li/a");
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
            .Select(chapter =>
            {
                var reg = new Regex(@"Vol ([0-9]{1,}) ");
                var match = reg.Match(chapter!.Title);
                var volNum = match.Success ? match.Groups[1].Value : "1";
                return (chapter!, volNum);
            })
            .ToArray();

        if (chapters.Length == 0) yield break;

        var images = doc.DocumentNode
            .SelectNodes("//div/figure[@class='aligncenter size-large']/img")?
            .Select(t => t.GetAttributeValue("src", string.Empty))
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!.Split('?').First())
            .ToArray() ?? [];

        var groupedChapters = chapters
            .OrderBy(t => t.volNum)
            .GroupBy(t => t.volNum);

        foreach (var group in groupedChapters)
        {
            yield return new SourceVolume
            {
                Title = $"Volume {group.Key}",
                Url = url,
                Chapters = group.Select(t => t.Item1).ToArray(),
                Inserts = images,
                Forwards = images
            };
        }
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
            .SelectNodes("//span[@class='next-chapter chapter-buttons']/a")?
            .FirstOrDefault(t => t is not null && t.InnerText.ToLower().Trim().Contains("next"))?
            .GetAttributeValue("href", string.Empty);

        return new SourceChapter(bookTitle, title, content, nextUrl ?? string.Empty, url);
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return null;
        }

        var title = doc.DocumentNode.SelectSingleNode("//header[@class='entry-header']/h1[@class='entry-title']")?.InnerHtml?.HTMLDecode();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogError("Failed to get series title: {url}", url);
            return null;
        }

        var (description, image) = GetValues(doc);

        var volumes = GetVolumes(url, doc);
        var firstChap = volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url;
        return new TempSeriesInfo(title, description, [], image, firstChap, [], []);
    }

    public (string? desc, string? image) GetValues(HtmlDocument document)
    {
        var contentNode = document.DocumentNode.SelectSingleNode("//div[@class='entry-content']");
        if (contentNode is null)
        {
            _logger.LogError("Failed to get series content");
            return (null, null);
        }

        var image = document.DocumentNode
            .SelectSingleNode("//div/figure[@class='aligncenter size-large']/img")?
            .GetAttributeValue("src", string.Empty)
            .Split('?').First();

        var traverser = new HtmlTraverser(contentNode);

        _ = traverser.MoveUntil(t => t.Name == "p" && t.InnerText.ToLower().Contains("description"));

        var desc = string.Join("<br>", traverser
            .EverythingUntil(t => t.NodeType == HtmlNodeType.Element && t.Name != "p")
            .Select(t => t.InnerText?.HTMLDecode()));

        return (string.IsNullOrWhiteSpace(desc) ? null : desc,
            string.IsNullOrWhiteSpace(image) ? null : image);
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
