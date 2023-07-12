namespace CardboardBox.LightNovel.Core.Sources;

public interface INyxSourceService : ISourceVolumeService { }

public class NyxSourceService : INyxSourceService
{
    private readonly IApiService _api;

    public string Name => "nyx";

    public string RootUrl => "https://nyx-translation.com";

    public NyxSourceService(IApiService api)
    {
        _api = api;
    }

    public (TempSeriesInfo? data, HtmlTraverser? traverser) FromDoc(HtmlDocument doc)
    {
        static IEnumerable<(string key, string[] value)> HandleAttributes(HtmlNode? node)
        {
            if (node == null) yield break;

            var tr = new HtmlTraverser(node);

            while (tr.Valid)
            {
                var entry = string.Join("", tr
                    .EverythingUntil("br")
                    .Select(x => x.InnerText.HTMLDecode().Trim()));
                
                var split = entry.Split(':');
                var key = split.First().Trim();
                var value = string.Join(":", split.Skip(1))
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToArray();

                yield return (key, value);
            }
        }

        var title = doc.InnerText("//header[@class='entry-header']/h1");
        if (string.IsNullOrEmpty(title)) return (null, null);

        var traverser = new HtmlTraverser(doc, "//div[@class='entry-content']");

        if (!traverser.Valid) return (null, null);

        var cover = traverser.MoveUntil("figure")?.FirstChild?.GetAttributeValue("src", "");
        _ = traverser.MoveUntil("h3")?.InnerText?.Trim();

        var attrs = HandleAttributes(traverser.MoveUntil("p")).ToArray();
        var genres = attrs.FirstOrDefault(x => x.key == "Genre").value;
        var author = attrs.FirstOrDefault(x => x.key == "Author").value;
        var tags = attrs.FirstOrDefault(x => x.key == "Type").value;

        var desc = traverser.AfterUntil(
            t => t.InnerText == "Description", 
            t => t.InnerText == "Alternative Name(s)")
            .ToArray()
            .Join(true)
            .Trim();

        var altTitles = traverser
            .MoveUntil("p")?
            .ChildNodes
            .Select(t => t.InnerText.HTMLDecode())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();

        var firstChap = traverser.MoveUntil("h5")?.Copy().Attribute("//a", "href");

        var back = traverser.BackUntil(t => t.InnerText == "Table of Contents");

        return (new TempSeriesInfo(
            title, 
            desc,
            author,
            cover,
            firstChap,
            genres,
            tags
        ), traverser);
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var doc = await _api.GetHtml(url);
        if (doc == null) return null;

        var (res, _) = FromDoc(doc);
        return res;
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
    {
        if (string.IsNullOrEmpty(seriesUrl)) yield break;

        var doc = await _api.GetHtml(seriesUrl);
        if (doc == null) yield break;

        var (res, traverser) = FromDoc(doc);
        if (res == null || traverser == null) yield break;

        var volReg = new Regex(@"^Volume (\d+)(:?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string? volume = null;
        string? chapter = null;
        var chaps = new List<SourceChapterItem>();
        var images = new List<string>();
        while(traverser.Valid)
        {
            var (node, index) = traverser.UntilOneOf(
                t => volReg.IsMatch(t.InnerText.HTMLDecode()), //0
                t => t.Name == "p", //1
                t => t.Name == "h5", //2
                t => t.Name == "div"); //3

            if (node == null || index == -1) break;

            var type = (VolumeMarker)index;
            var title = node.InnerText.HTMLDecode().Trim();
            var copy = node.Copy();

            if (type == VolumeMarker.Stopper || 
                type == VolumeMarker.NotFound) break;

            if (type == VolumeMarker.Volume)
            {
                if (string.IsNullOrEmpty(volume))
                {
                    volume = title;
                    continue;
                }

                yield return new SourceVolume
                {
                    Title = volume ?? "",
                    Url = seriesUrl,
                    Chapters = chaps.ToArray(),
                    Forwards = images.ToArray()
                };
                chaps.Clear();
                images.Clear();
                volume = node.InnerText.HTMLDecode().Trim().Trim(':');
                chapter = null;
                continue;
            }

            if (title.StartsWith("Illustration"))
            {
                var illUrl = copy.Attribute("//a", "href");
                if (string.IsNullOrEmpty(illUrl)) continue;

                images.AddRange(await GetImages(illUrl));
                continue;
            }

            if (type == VolumeMarker.Chapter)
            {
                chapter = node.InnerText.HTMLDecode().Trim();
                continue;
            }

            copy.SelectNodes("//a")
                .Select(t => (t.GetAttributeValue("href", ""), t.InnerText.HTMLDecode().Trim()))
                .Where(t => !string.IsNullOrEmpty(t.Item1) && !string.IsNullOrEmpty(t.Item2))
                .Each(t =>
                {
                    var title = $"{chapter} {t.Item2}".Trim();
                    chaps.Add(new SourceChapterItem
                    {
                        Title = title,
                        Url = t.Item1
                    });
                });
        }

        if (string.IsNullOrEmpty(volume) || 
            chaps.Count == 0) yield break;

        yield return new SourceVolume
        {
            Title = volume ?? "",
            Url = seriesUrl,
            Chapters = chaps.ToArray(),
            Forwards = images.ToArray()
        };
    }

    public async Task<string[]> GetImages(string url)
    {
        if (string.IsNullOrEmpty(url)) return Array.Empty<string>();

        var doc = await _api.GetHtml(url);
        if (doc == null) return Array.Empty<string>();

        var content = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']")?.Copy();
        if (content == null) return Array.Empty<string>();

        return content.SelectNodes("//img")
            .Select(t => t.GetAttributeValue("src", ""))
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray();
    }

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var doc = await _api.GetHtml(url);
        if (doc == null) return null;

        return GetChapter(doc, bookTitle, url).chap;
    }

    public (SourceChapter? chap, string? mainurl) GetChapter(HtmlDocument doc, string bookTitle, string url)
    {
        var title = doc.InnerHtml("//h1[@class='entry-title']");
        if (string.IsNullOrEmpty(title)) return (null, null);

        var content = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']");
        if (content == null) return (null, null);

        static string? getLink(HtmlNode node, string text)
        {
            var copy = node.Copy();
            var firstPass = copy
                .SelectSingleNode($"//a[contains(text(), '{text}')]")?
                .GetAttributeValue("href", "")?
                .Trim();

            if (firstPass != null) return firstPass;

            return copy.SelectNodes("//a")
                .FirstOrDefault(t => t.InnerText.ToLower().Contains(text.ToLower()))?
                .GetAttributeValue("href", "")?
                .Trim();
        }

        static string? getHrefLink(HtmlNode node, string href)
        {
            return node
                .Copy()
                .SelectSingleNode($"//a[contains(@href, '{href}')]")?
                .GetAttributeValue("href", "")?
                .Trim();
        }

        var traverser = new HtmlTraverser(content);
        var nodes = traverser
            .EverythingUntil(t => t.Name == "p" && getLink(t, "Next") != null)
            .Where(t =>
            {
                var inner = t.InnerText.HTMLDecode().Trim().ToLower();
                if (inner.Contains("sponsored chapter by patreon"))
                    return false;

                if (t.Name == "div" && string.IsNullOrWhiteSpace(inner))
                    return false;

                if (getHrefLink(t, "patreon.com") != null)
                    return false;

                if (t.InnerHtml.Contains("<img"))
                    t.CleanupNode();

                return true;
            })
            .ToArray();

        if (nodes.Length == 0) return (null, null);

        var mainUrl = getLink(content, "Table of Contents");
        var next = getLink(content, "Next") ?? string.Empty;

        var data = nodes.Join();

        return (new SourceChapter(bookTitle, title, data, next, url), mainUrl);
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        if (string.IsNullOrEmpty(firstUrl)) yield break;

        var fcDoc = await _api.GetHtml(firstUrl);
        if (fcDoc == null) yield break;

        var (fc, mainUrl) = GetChapter(fcDoc, "", firstUrl);
        if (fc == null || string.IsNullOrWhiteSpace(mainUrl)) yield break;

        var series = await GetSeriesInfo(mainUrl);
        if (series == null) yield break;

        fc.BookTitle = series.Title;
        yield return fc;

        var url = fc.NextUrl;
        while(true)
        {
            var chap = await GetChapter(url, series.Title);
            if (chap == null) yield break;

            yield return chap;

            if (string.IsNullOrEmpty(chap.NextUrl))
                break;

            url = chap.NextUrl;
        }
    }

    public string SeriesFromChapter(string url)
    {
        throw new NotImplementedException();
    }

    public enum VolumeMarker
    {
        NotFound = -1,
        Volume = 0,
        Chapter = 1,
        Pages = 2,
        Stopper = 3
    }
}
