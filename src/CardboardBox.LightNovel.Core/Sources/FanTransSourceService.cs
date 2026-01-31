
namespace CardboardBox.LightNovel.Core.Sources;

public interface IFanTransSourceService : ISourceVolumeService { }

internal class FanTransSourceService : RatedSource, IFanTransSourceService
{
    private readonly IApiService _api;
    private readonly ILogger _logger;

    public string Name => "fan-translations";

    public string RootUrl => "https://fanstranslations.com";

    public override int MaxRequestsBeforePauseMin => 4;
    public override int MaxRequestsBeforePauseMax => 7;
    public override int PauseDurationSecondsMin => 30;
    public override int PauseDurationSecondsMax => 35;

    private readonly Dictionary<string, SourceVolume[]> _volumes = new();

    public FanTransSourceService(
        IApiService api,
        ILogger<FanTransSourceService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<SourceVolume[]> GetVolumes(string url)
    {
        url = url.Trim('/').ToLower();

        if (_volumes.TryGetValue(url, out var volumes))
            return volumes;

        return _volumes[url] = await Volumes(url).ToArrayA();
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
    {
        var safeSeriesUrl = seriesUrl.Trim('/').ToLower();
        if (_volumes.TryGetValue(safeSeriesUrl, out var volumes))
        {
            foreach (var volume in volumes)
                yield return volume;
            yield break;
        }

        var chaptersUrl = $"{seriesUrl.Trim('/')}/ajax/chapters";
        var doc = await _api.PostHtml(chaptersUrl);
        if (doc is null)
        {
            _logger.LogError("Failed to get series volumes: {url}", seriesUrl);
            yield break;
        }

        var chapters = doc
            .DocumentNode
            .SelectNodes("//ul[@class='main version-chap no-volumn']/li/a");

        if (chapters is null)
        {
            _logger.LogError("Failed to get series volumes: {url}", seriesUrl);
            yield break;
        }

        string fallbackName = "Volume 1";
        string? volumeNumber = null;
        var outputChaps = new List<SourceChapterItem>();

        foreach(var chapter in chapters.Reverse())
        {
            if (chapter is null) continue;

            var name = chapter.InnerText?.HTMLDecode()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;

            var url = chapter.GetAttributeValue("href", string.Empty);

            var volume = GetVolume(name);
            if (volume != volumeNumber)
            {
                if (outputChaps.Count > 0)
                {
                    yield return new SourceVolume
                    {
                        Chapters = outputChaps.ToArray(),
                        Title = volumeNumber ?? fallbackName,
                        Url = seriesUrl,
                    };
                    outputChaps.Clear();
                }

                volumeNumber = volume;
            }

            outputChaps.Add(new SourceChapterItem
            {
                Title = name,
                Url = url,
            });
        }

        if (chapters.Count > 0)
        {
            yield return new SourceVolume
            {
                Title = volumeNumber ?? fallbackName,
                Url = seriesUrl,
                Chapters = outputChaps.ToArray()
            };
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

        var body = doc.DocumentNode.SelectSingleNode("//div[@class='site-content']/div/div[@class='content-area']/div[@class='container']/div[@class='row']/div");
        if (body is null)
        {
            _logger.LogError("Failed to get chapter body: {url}", url);
            return null;
        }

        var title = GetChapterTitle(body, url);
        if (title is null) return null;

        var next = GetNextChapter(doc);

        var reader = body.SelectSingleNode(".//div[@class='entry-content']//div[@class='reading-content']/div[@class='text-left']");
        if (reader is null)
        {
            _logger.LogError("Failed to get chapter reader: {url}", url);
            return null;
        }

        var removals = new[] { "div", "script", "span" };
        foreach(var child in reader.ChildNodes.ToArray())
        {
            if (child.NodeType != HtmlNodeType.Element)
            {
                child.Remove();
                continue;
            }

            if (removals.Contains(child.Name))
            {
                child.Remove();
                continue;
            }
        }

        var content = reader.InnerHtml?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogError("Failed to get chapter content: {url}", url);
            return null;
        }

        return new SourceChapter
        {
            BookTitle = bookTitle,
            Content = content,
            ChapterTitle = title,
            NextUrl = next ?? string.Empty,
            Url = url,
        };
    }

    public string? GetNextChapter(HtmlDocument doc)
    {
        var node = doc.DocumentNode.SelectNodes("//div[@class='entry-header footer']/div[@class='wp-manga-nav']//a");
        if (node is null) return null;

        var nextNode = node.FirstOrDefault(t => t.InnerText?.ToLower().Contains("next") ?? false);
        if (nextNode is null) return null;

        var nextUrl = nextNode.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(nextUrl)) return null;

        if (nextUrl.Contains("/coming-soon/")) return null;

        return nextUrl;
    }

    public string? GetChapterTitle(HtmlNode body, string url)
    {
        var crumbs = body.SelectNodes(".//div[@class='entry-header_wrap']/div[@class='c-breadcrumb-wrapper']//ol[@class='breadcrumb']/li");
        if (crumbs is null)
        {
            _logger.LogError("Failed to get chapter title for {url}", url);
            return null;
        }

        var title = crumbs.LastOrDefault()?.InnerText?.HTMLDecode()?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogError("Failed to get chapter title for {url}", url);
            return null;
        }

        return title;
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

        //string url = firstUrl;
        //while (true)
        //{
        //    var chapter = await GetChapter(url, string.Empty);
        //    if (chapter is null)
        //    {
        //        _logger.LogError("Failed to get chapter: {url}", url);
        //        yield break;
        //    }

        //    yield return chapter;
        //    if (string.IsNullOrEmpty(chapter.NextUrl) || chapter.NextUrl.EndsWith("/coming-soon/"))
        //        break;
        //    url = chapter.NextUrl;
        //}
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await _api.GetHtml(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return null;
        }

        var header = doc.DocumentNode.SelectSingleNode("//div[@class='post-title']");
        if (header is null)
        {
            _logger.LogError("Failed to get series header: {url}", url);
            return null;
        }

        header = header.ParentNode;

        var title = doc.InnerText("//div[@class='post-title']/h1")?.HTMLDecode()?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogError("Failed to get series title: {url}", url);
            return null;
        }

        var image = GetImage(header, url);
        var (authors, genres, tags) = Attributes(header);
        var description = GetDescription(doc, url);

        var volumes = await GetVolumes(url);
        var firstChap = volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url;

        return new TempSeriesInfo(title, description, authors, image, firstChap, genres, tags);
    }

    public (string[] authors, string[] genres, string[] tags) Attributes(HtmlNode header)
    {
        var authors = new List<string>();
        var genres = new List<string>();
        var tags = new List<string>();

        var items = header.SelectNodes(".//div[@class='post-content_item']");
        foreach (var item in items)
        {
            var type = item.InnerText(".//div[@class='summary-heading']/h5")?.HTMLDecode()?.Trim().ToLower();
            if (string.IsNullOrEmpty(type)) continue;

            var content = item.SelectSingleNode("./div[@class='summary-content']");
            if (content is null) continue;

            var links = content.SelectNodes(".//a[@rel='tag']")?
                .Where(t => t is not null)
                .Select(t =>
                {
                    var link = t.GetAttributeValue("href", string.Empty);
                    var text = t.InnerText?.HTMLDecode() ?? string.Empty;
                    return (link, text);
                })
                .ToArray() ?? Array.Empty<(string link, string text)>();

            if (type.Contains("author"))
            {
                authors.AddRange(links.Select(l => l.text));
                continue;
            }

            if (type.Contains("genre"))
            {
                genres.AddRange(links.Select(t => t.text));
                continue;
            }

            if (type.Contains("tag"))
            {
                tags.AddRange(links.Select(t => t.text));
                continue;
            }
        }

        return (authors.ToArray(), genres.ToArray(), tags.ToArray());
    }

    public string? GetVolume(string name)
    {
        var checks = new Regex[]
        {
            new Regex("volume\\s+(\\d+)", RegexOptions.IgnoreCase),
            new Regex("\\[vol\\.\\s*(\\d+)\\]", RegexOptions.IgnoreCase),
        };

        foreach (var check in checks)
        {
            var match = check.Match(name);
            if (match.Success)
                return match.Groups[1].Value;
        }

        return null;
    }

    public string? GetImage(HtmlNode header, string url)
    {
        var image = header.SelectSingleNode(".//div[@class='summary_image']/a/img");
        if (image is null)
        {
            _logger.LogWarning("Couldn't find image node: {url}", url);
            return null;
        }

        var img = image.GetAttributeValue("src", string.Empty);
        if (string.IsNullOrWhiteSpace(img))
        {
            _logger.LogWarning("Couldn't find image src: {url}", url);
            return null;
        }

        if (!img.EndsWith("?crop=1"))
            return img;

        var alt = image.GetAttributeValue("alt", string.Empty);
        if (string.IsNullOrWhiteSpace(alt))
        {
            _logger.LogWarning("Couldn't find image alt: {url}", url);
            return img;
        }

        var parts = img.Split('/');
        var filename = parts.LastOrDefault()?.Split('?').FirstOrDefault();
        var path = string.Join("/", parts.SkipLast(1));

        if (string.IsNullOrWhiteSpace(filename))
        {
            _logger.LogWarning("Couldn't find image filename: {url}", url);
            return img;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Couldn't find image path: {url}", url);
            return img;
        }

        var ext = Path.GetExtension(filename).Trim('.');
        return $"{path}/{alt}.{ext}";
    }

    public string? GetDescription(HtmlDocument doc, string url)
    {
        var description = doc
            .DocumentNode
            .SelectSingleNode("//div[@class='description-summary']/div[@class='summary__content show-more']");
        if (description is null)
        {
            _logger.LogWarning("Could not find description for {url}", url);
            return null;
        }

        foreach(var node in description.ChildNodes.ToArray())
        {
            if (node.Name != "p")
                node.Remove();
        }

        return description.InnerHtml?.HTMLDecode();
    }

    public string SeriesFromChapter(string url)
    {
        return string.Join("/", url.Split('/').SkipLast(1));
    }
}
