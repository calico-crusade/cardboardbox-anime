
namespace CardboardBox.LightNovel.Core.Sources;

public interface IHeadCanonTLSourceService : ISourceVolumeService 
{
    Task<(TempSeriesInfo? info, SourceVolume[] vols)> Actual(string url);
}

internal class HeadCanonTLSourceService : IHeadCanonTLSourceService
{
    private readonly IApiService _api;
    private readonly ILogger _logger;

    public string Name => "HeadCanonTL";

    public string RootUrl => "https://headcanontl.wordpress.com";

    private readonly Dictionary<string, SourceVolume[]> _volumes = new();
    private readonly Dictionary<string, TempSeriesInfo> _info = new();

    public HeadCanonTLSourceService(
        IApiService api, 
        ILogger<HeadCanonTLSourceService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public HtmlNode? GetContentNode(HtmlDocument doc, string url)
    {
        var content = doc.DocumentNode.SelectSingleNode("//main[@class='wp-block-group is-layout-flow wp-block-group-is-layout-flow']" +
            "/div[@class='entry-content wp-block-post-content is-layout-constrained wp-block-post-content-is-layout-constrained']");
        if (content is null)
        {
            _logger.LogError("Couldn't find series content: {url}", url);
            return null;
        }

        return content;
    }

    public async Task<(TempSeriesInfo? info, SourceVolume[] vols)> Actual(string url)
    {
        var def = ((TempSeriesInfo?)null, Array.Empty<SourceVolume>());
        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series volumes: {url}", url);
            return def;
        }

        var title = doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText?
            .Replace("&#8211;Headcanon TL", "")
            .HTMLDecode()
            .Trim();
        if (string.IsNullOrEmpty(title))
        {
            _logger.LogError("Failed to get series title: {url}", url);
            return def;
        }

        var content = GetContentNode(doc, url);
        if (content is null) return def;

        var traverser = new HtmlTraverser(content);

        var mainImage = traverser.MoveUntil("figure");
        if (mainImage is null)
        {
            _logger.LogWarning("Failed to get series main image: {url}", url);
            return def;
        }

        var firstImage = mainImage.SelectSingleNode("./img")?.GetAttributeValue("data-orig-file", string.Empty);
        if (string.IsNullOrEmpty(firstImage))
        {
            _logger.LogWarning("Failed to get series first image: {url}", url);
            return def;
        }

        var volUrl = firstImage;
        var description = Description(traverser, url);
        var authors = Attributions(traverser, url);

        TempSeriesInfo? info = null;
        var vols = new List<SourceVolume>();
        while (traverser.Valid)
        {
            var volTitle = traverser.MoveUntil(t => t.InnerText?.Trim()?.StartsWith("volume", StringComparison.OrdinalIgnoreCase) ?? false);
            var volTitleText = volTitle?.InnerText?.HTMLDecode();
            if (volTitle is null || string.IsNullOrEmpty(volTitleText)) break;

            var image = volTitle.SelectSingleNode(".//img")?.GetAttributeValue("data-orig-file", string.Empty);
            var imageUrl = string.IsNullOrEmpty(image) ? volUrl : image;

            var next = traverser.MoveUntil(t => new[] { "p", "div" }.Contains(t.Name));
            if (next is null) break;

            var chapters = next.SelectNodes(".//a")
                ?.Select(t =>
                {
                    if (t is null) return null;

                    var href = t.GetAttributeValue("href", string.Empty);
                    var title = t.InnerText?.HTMLDecode()?.Trim();
                    if (string.IsNullOrEmpty(href) ||
                        string.IsNullOrEmpty(title)) return null;

                    return new SourceChapterItem
                    {
                        Title = title,
                        Url = href
                    };
                })
                .Where(t => t is not null)
                .Select(t => t!)
                .ToArray() ?? Array.Empty<SourceChapterItem>();

            if (chapters.Length == 0) continue;

            vols.Add(new SourceVolume
            {
                Title = volTitleText,
                Url = chapters.First().Url,
                Chapters = chapters,
                Inserts = new[] { imageUrl },
                Forwards = new[] { imageUrl }
            });

            info ??= new TempSeriesInfo(title, description, authors, volUrl, chapters.First().Url, Array.Empty<string>(), Array.Empty<string>());
        }

        return (info, vols.ToArray());
    }

    public string[] Attributions(HtmlTraverser traverser, string url)
    {
        var item = traverser.MoveUntil("p");
        if (item is null)
        {
            _logger.LogWarning("Failed to get series attributions: {url}", url);
            return Array.Empty<string>();
        }

        return item.SelectNodes("./a")
            ?.Select(a => a.InnerText?.HTMLDecode())
            ?.Where(t => !string.IsNullOrEmpty(t))
            ?.Select(t => t!.Trim())
            ?.ToArray() ?? Array.Empty<string>();
    }

    public string? Description(HtmlTraverser traverser, string url)
    {
        var quote = traverser.MoveUntil("blockquote");
        if (quote is null)
        {
            _logger.LogWarning("Failed to get series quote: {url}", url);
            return null;
        }

        foreach(var child in quote.ChildNodes.ToArray())
        {
            if (child.Name == "p") continue;

            child.Remove();
        }

        return quote.InnerHtml.HTMLDecode();
    }

    public async Task<(TempSeriesInfo? info, SourceVolume[] vols)> GetInfo(string url)
    {
        var def = ((TempSeriesInfo?)null, Array.Empty<SourceVolume>());
        url = url.Trim('/').ToLower();

        if (_info.TryGetValue(url, out var info1) &&
            _volumes.TryGetValue(url, out var vols1))
            return (info1, vols1);

        var (info, vols) = await Actual(url);
        if (info is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return def;
        }

        if (vols is null || vols.Length == 0)
        {
            _logger.LogError("Failed to get series volumes: {url}", url);
            return def;
        }

        _info[url] = info;
        _volumes[url] = vols;
        return (info, vols);
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
    {
        var (_, vols) = await GetInfo(seriesUrl);
        if (vols.Length == 0) yield break;

        foreach (var vol in vols)
            yield return vol;
    }

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get chapter: {url}", url);
            return null;
        }

        var title = doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText?
            .Replace("&#8211;", "")
            .Replace("Headcanon TL", "")
            .HTMLDecode()
            .Trim();
        if (string.IsNullOrEmpty(title))
        {
            _logger.LogError("Failed to get chapter title: {url}", url); 
            return null;
        }

        var content = GetContentNode(doc, url);
        if (content is null) return null;

        string? nextUrl = null;
        var removes = new[] { "nav", "script", "span", "hr" };

        foreach (var child in content.ChildNodes.ToArray())
        {
            if (removes.Contains(child.Name?.ToLower()))
            {
                child.Remove();
                continue;
            }

            if (child.Name == "figure")
            {
                var img = child.SelectSingleNode(".//img")?.GetAttributeValue("data-orig-file", string.Empty);
                if (string.IsNullOrEmpty(img))
                {
                    child.Remove();
                    continue;
                }

                var node = HtmlNode.CreateNode($"<img src=\"{img}\" />");
                content.ReplaceChild(node, child);
                continue;
            }

            if (string.IsNullOrWhiteSpace(child.InnerText?.Trim()))
            {
                child.Remove();
                continue;
            }

            if (child.Name != "div") continue;

            child.Remove();

            var anchors = child.SelectNodes(".//a")?
                .Select(t =>
                {
                    var href = t.GetAttributeValue("href", string.Empty);
                    var text = t.InnerText?.HTMLDecode()?.Trim();
                    return (href, text);
                });
            if (anchors is null) continue;

            var next = anchors.FirstOrDefault(t => t.text?.ToLower().Contains("next") ?? false).href;
            if (string.IsNullOrEmpty(next)) continue;

            nextUrl ??= next;
        }

        var pageContent = content.InnerHtml.HTMLDecode();
        return new SourceChapter
        {
            BookTitle = bookTitle,
            ChapterTitle = title,
            Content = pageContent,
            NextUrl = nextUrl ?? string.Empty,
            Url = url
        };
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        string url = firstUrl;
        while (true)
        {
            var chapter = await GetChapter(url, string.Empty);
            if (chapter is null)
            {
                _logger.LogError("Failed to get chapter: {url}", url);
                yield break;
            }

            yield return chapter;
            if (string.IsNullOrEmpty(chapter.NextUrl) || chapter.NextUrl.EndsWith("/coming-soon/"))
                break;
            url = chapter.NextUrl;
        }
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var (seriesInfo, _) = await GetInfo(url);
        return seriesInfo;
    }

    public string SeriesFromChapter(string url)
    {
        throw new NotImplementedException();
    }
}
