namespace CardboardBox.LightNovel.Core.Sources;

using System;
using Utilities;

public interface IBakaPervertSourceService : ISourceVolumeService { }

internal class BakaPervertSourceService : IBakaPervertSourceService
{
    private readonly IApiService _api;
    private readonly ILogger<BakaPervertSourceService> _logger;

    public string Name => "bakapervert";

    public string RootUrl => "https://bakapervert.wordpress.com";

    private string? _volumesUrl;
    private SourceVolume[]? _volumes;

    public BakaPervertSourceService(
        IApiService api,
        ILogger<BakaPervertSourceService> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<SourceVolume[]> GetVolumes(string url)
    {
        if (_volumesUrl == url && _volumes != null)
            return _volumes;

        _volumesUrl = url;
        return _volumes = await Volumes(url).ToArrayAsync();
    }

    public static string SubUrl(string url)
    {
        var subs = new Dictionary<string, string>
        {
            ["https://bakapervert.wordpress.com/2020/09/05/arifureta-390/"] = "https://bakapervert.wordpress.com/arifureta-chapter-390/",
            ["https://bakapervert.wordpress.com/2017/12/03/arifureta-290-finished/"] = "https://bakapervert.wordpress.com/arifureta-chapter-290/",
            ["https://bakapervert.wordpress.com/2023/03/11/arifureta-2nd-season-blu-ray-ss-chapter-5/"] = "https://bakapervert.wordpress.com/arifureta-2nd-season-blu-ray-ss-chapter-5/",
            ["https://bakapervert.wordpress.com/2023/02/05/arifureta-animate-limited-ss/"] = "https://bakapervert.wordpress.com/arifureta-animate-limited-ss/",
        };

        if (subs.TryGetValue(url, out var sub))
            return sub;

        return url;
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
    {
        var doc = await _api.GetHtml(seriesUrl);
        if (doc == null) yield break;

        var entryContent = doc.DocumentNode.SelectSingleNode("//div[@id='content']/div/div[@class='entry-content']");
        if (entryContent == null) yield break;

        var children = entryContent.ChildNodes
            .Where(t => t.NodeType != HtmlNodeType.Text)
            .Skip(8);

        var title = "Arifureta: Side Story";
        var chapters = new List<SourceChapterItem>();

        foreach(var child in children)
        {
            if (child.Name == "h2")
            {
                if (chapters.Count > 0)
                {
                    yield return new SourceVolume
                    {
                        Title = title,
                        Url = seriesUrl,
                        Chapters = chapters.ToArray()
                    };
                    chapters.Clear();
                }

                title = child.InnerText?.HTMLDecode() ?? string.Empty;

                if (string.IsNullOrEmpty(title))
                {
                    _logger.LogError("Failed to get volume title: {element}", child.InnerHtml);
                    throw new Exception("Failed to get volume title");
                }

                continue;
            }

            if (child.Name != "p") continue;

            var links = child.SelectNodes(".//a");
            foreach(var link in links)
            {
                chapters.Add(new SourceChapterItem
                {
                    Title = link.InnerText?.HTMLDecode() ?? string.Empty,
                    Url = link.GetAttributeValue("href", string.Empty)
                });
            }
        }

        if (chapters.Count > 0)
        {
            yield return new SourceVolume
            {
                Title = title,
                Url = seriesUrl,
                Chapters = chapters.ToArray()
            };
        }
    }

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {


        var doc = await _api.GetHtml(SubUrl(url));
        if (doc == null)
        {
            _logger.LogError("Failed to get chapter: {url}", url);
            return null;
        }

        var title = doc.InnerText("//h1[@class='entry-title']")?.HTMLDecode();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogError("Failed to get chapter title: {url}", url);
            return null;
        }

        var content = doc.InnerHtml("//div[@class='entry-content']");
        if (string.IsNullOrWhiteSpace(content)) 
        {
            _logger.LogError("Failed to get chapter content: {url}", url);
            return null;
        }

        doc = new HtmlDocument();
        doc.LoadHtml(content);

        var jpPost = doc.DocumentNode.SelectSingleNode("//div[@id='jp-post-flair']");
        jpPost?.Remove();

        if (_volumes is null)
        {
            var toc = doc.DocumentNode
               .SelectNodes("//p/a")
               .FirstOrDefault(t => t.InnerText.Trim().ToLower() == "table of contents");
            var tocLink = toc?.GetAttributeValue("href", string.Empty)?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(tocLink))
            {
                _logger.LogError("Failed to get table of contents link - Link was empty: {url}", url);
                return null;
            }

            toc?.ParentNode.Remove();
            await GetVolumes(tocLink);
        }

        if (_volumes is null)
        {
            _logger.LogError("Failed to get volumes: {url}", url);
            return null;
        }
       
        var volumes = _volumes.SelectMany(t => t.Chapters)
            .Select(t => t.Url)
            .ToArray();

        var output = doc.DocumentNode.InnerHtml;
        var index = Array.IndexOf(volumes, url);
        string? next = null;
        if (index >= 0 && index < volumes.Length - 1)
            next = volumes[index + 1];

        return new SourceChapter
        {
            ChapterTitle = title,
            Content = output,
            NextUrl = next ?? string.Empty,
            Url = url,
            BookTitle = bookTitle
        };
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        string url = firstUrl;
        while(true)
        {
            var chapter = await GetChapter(url, string.Empty);
            if (chapter is null)
            {   
                _logger.LogError("Failed to get chapter: {url}", url);
                yield break;
            }

            if (string.IsNullOrEmpty(_volumesUrl))
            {
                _logger.LogError("Failed to get volumes url for {url}", url);
                yield break;
            }

            var volumes = await GetVolumes(_volumesUrl);
            var volume = volumes.FirstOrDefault(t => t.Chapters.Any(c => c.Url.Equals(url, StringComparison.OrdinalIgnoreCase)));
            chapter.BookTitle = volume?.Title ?? string.Empty;

            yield return chapter;
            if (string.IsNullOrEmpty(chapter.NextUrl) || chapter.NextUrl.EndsWith("/coming-soon/"))
                break;
            url = chapter.NextUrl;
        }
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await _api.GetHtml(url);
        if (doc == null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return null;
        }

        var title = doc.InnerText("//h1[@class='entry-title']")?.HTMLDecode();
        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogError("Failed to get series title: {url}", url);
            return null;
        }

        var entryContent = doc.DocumentNode.SelectSingleNode("//div[@id='content']/div/div[@class='entry-content']");
        if (entryContent == null) return null;

        var children = entryContent.ChildNodes
            .Where(t => t.NodeType != HtmlNodeType.Text)
            .Take(8);

        string description = "";
        string? image = null;
        var authors = new List<string>();

        foreach(var child in children)
        {
            if (child.Name != "p") continue;

            if (child.InnerText.Trim().StartsWith("Author:"))
            {
                authors.AddRange(child.InnerText.Split(":").Last().Split(","));
                continue;
            }

            var images = child.SelectNodes(".//img");
            if (images != null && images.Count > 0)
            {
                image = images.First().GetAttributeValue("data-orig-file", string.Empty);
                continue;
            }

            description += child.InnerText.HTMLDecode();
        }

        var volumes = await GetVolumes(url);
        var firstChap = volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url;

        return new TempSeriesInfo(title, description, authors.ToArray(), image, firstChap, Array.Empty<string>(), Array.Empty<string>());
    }

    public string SeriesFromChapter(string url)
    {
        throw new NotImplementedException();
    }
}
