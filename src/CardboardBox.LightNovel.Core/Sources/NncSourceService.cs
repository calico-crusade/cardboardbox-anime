namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;

public interface INncSourceService : ISourceService { }

public class NncSourceService : INncSourceService
{
    private readonly IApiService _api;
    private readonly ILogger _logger;
    private readonly INovelUpdatesService _nus;

    public string Name => "novelonomicon";

    public string RootUrl => "https://novelonomicon.com";

    public NncSourceService(
        IApiService api, 
        ILogger<NncSourceService> logger, 
        INovelUpdatesService nus)
    {
        _api = api;
        _logger = logger;
        _nus = nus;
    }

    public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        var (info, chapters) = await _nus.GetChapters(SeriesFromChapter(firstUrl));
        if (info == null || chapters.Length == 0) yield break;

        string? startUrl = firstUrl;

        if (firstUrl.ToLower().Contains("novelupdates"))
            startUrl = await _nus.GetChapterUrl(chapters.AReverse().First());

        if (startUrl == null) yield break;

        string chapUrl = startUrl;

        while(true)
        {
            var doc = await _api.GetHtml(chapUrl);
            if (doc == null) yield break;

            var chap = GetChapter(doc, info, chapUrl);
            if (chap == null) yield break;

            if (!string.IsNullOrEmpty(chap.Content) ||
                !chap.ChapterTitle.ToLower().Contains("illustrations"))
                yield return chap;

            if (string.IsNullOrEmpty(chap.NextUrl)) yield break;

            chapUrl = chap.NextUrl;
        }
    }

    public static SourceChapter? GetChapter(HtmlDocument doc, TempSeriesInfo info, string url)
    {
        var title = doc.InnerText("//h1[@class='tdb-title-text']");
        if (string.IsNullOrEmpty(title)) return null;

        var content = Content(doc);       
        return new SourceChapter(
            info.Title,
            title,
            content ?? string.Empty,
            NextUrl(doc) ?? string.Empty,
            url);
    }

    public static string? NextUrl(HtmlDocument doc)
    {
        return doc.Attribute("//div[@class='tdb-next-post tdb-next-post-bg tdb-post-next']" +
            "/div[@class='td-module-container']" +
            "/div[@class='next-prev-title']" +
            "/a", "href");
    }

    public static string? Content(HtmlDocument doc)
    {
        var contentStart = doc.DocumentNode.SelectSingleNode(
            "//div[@class='td_block_wrap tdb_single_content tdi_50 td-pb-border-top td_block_template_1 td-post-content tagdiv-type']" +
            "/div[@class='tdb-block-inner td-fix-index']");
        if (contentStart == null) return null;

        var traverser = new HtmlTraverser(contentStart);

        var content = traverser.EverythingBut(t => t.GetAttributeValue("style", "").ToLower().Contains("text-align: center")).Join();
        return string.IsNullOrEmpty(content) ? null : content;
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var (series, chapters) = await _nus.GetChapters(SeriesFromChapter(url));
        if (series == null || chapters.Length == 0) return null;

        var first = await _nus.GetChapterUrl(chapters.AReverse().First());

        return new TempSeriesInfo(
            series.Title,
            series.Description,
            series.Authors,
            series.Image,
            first,
            series.Genre,
            series.Tags);
    }

    public string SeriesFromChapter(string url)
    {
        if (url.ToLower().Contains("novelupdates.com")) return url;

        var map = new Dictionary<string, string>
        {
            ["isekai-yururi-kikou"] = "https://www.novelupdates.com/series/isekai-yururi-kikou-raising-children-while-being-an-adventurer"
        };

        foreach(var (key, value) in map)
        {
            if (url.ToLower().Contains(key)) return value;
        }

        throw new NotImplementedException();
    }
}
