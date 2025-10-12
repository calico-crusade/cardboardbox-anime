namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface INovelBinSourceService : ISourceVolumeService { }

public class NovelBinSourceService(
    IFlareSolver _flare,
    ISmartReaderService _smart,
    ILogger<NovelBinSourceService> _logger) : FlareVolumeSource(_flare, _smart, _logger), INovelBinSourceService
{
    public override string Name => "novel-bin";

    public override string RootUrl => "https://novelbin.com";

    public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await Get(url, true);
        if (doc is null) return null;

        var description = doc.InnerText("//div[@role='tabpanel']/div[@class='desc-text']")?.Trim();

        string[] authors = [];
        string[] genres = [];
        string[] tags = [];
        var info = doc.DocumentNode.SelectNodes("//ul[@class='info info-meta']/li");
        foreach(var node in info)
        {
            var h3 = node.InnerText(".//h3")?.Replace(":", "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(h3)) continue;

            var items = node.SelectNodes(".//a")?
                .Select(x => x.InnerText?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != "See more »")
                .Select(t => t!)
                .ToArray() ?? [];

            switch (h3)
            {
                case "author": authors = items; break;
                case "genre": genres = items; break;
                case "tag": tags = items; break;
            }
        }

        var title = doc.InnerText("//div[@class='col-xs-12 col-sm-8 col-md-8 desc']//h3[@class='title']")?.Trim();
        if (string.IsNullOrWhiteSpace(title)) return null;

        var image = doc.Attribute("//div[@class='books']/div[@class='book']/img", "src")?.Trim();
        var fc = doc.Attribute("//a[@title='READ NOW']", "href")?.Trim();
        return new TempSeriesInfo(title, description, authors, image, fc, genres, tags);
    }

    public override string? NextUrl(HtmlDocument doc, string url)
    {
        return doc.Attribute("//a[@id='next_chap']", "href")?.Trim();
    }

    public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        var nid = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        var req = $"https://novelbin.com/ajax/chapter-archive?novelId={nid}";
        var content = await Get(req, true);
        if (content is null) yield break;

        var links = content.DocumentNode.SelectNodes("//ul[@class='list-chapter']/li/a")
            .Select(t => new SourceChapterItem
            {
                Title = t.InnerText?.HTMLDecode()?.Trim() ?? string.Empty,
                Url = t.GetAttributeValue("href", string.Empty)
            })
            .ToArray();
        if (links.Length == 0) yield break;

        yield return new SourceVolume
        {
            Title = "Volume 1",
            Url = url,
            Chapters = links
        };
    }

    public override string SeriesFromChapter(string url)
    {
        var regex = new Regex("https://novelbin.com/b/(.*?)/(.*?)");
        var match = regex.Match(url);
        if (!match.Success) return url;

        return $"https://novelbin.com/b/{match.Groups[1].Value}/";
    }
}
