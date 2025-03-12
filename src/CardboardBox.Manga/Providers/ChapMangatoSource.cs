namespace CardboardBox.Manga.Providers;

public interface IChapmanganatoSource : IMangaUrlSource { }

public class ChapmanganatoSource : IChapmanganatoSource
{
    public string HomeUrl => "https://www.natomanga.com";

    public string ChapterBaseUri => $"{HomeUrl}";

    public string MangaBaseUri => $"{HomeUrl}";

    public string Provider => "chapmanganato";

    private readonly IApiService _api;

    public ChapmanganatoSource(IApiService api)
    {
        _api = api;
    }

    public async Task<MangaChapterPages?> ChapterPages(string url)
    {
        var doc = await _api.GetHtml(url);
        if (doc == null) return null;

        var chapterId = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

        var title = doc.DocumentNode.SelectNodes(
            "//div[@class='breadcrumb breadcrumbs bred_doc']/p/span/a/span")
            ?.LastOrDefault()?.InnerText?.HTMLDecode() ?? string.Empty;

        var chapter = new MangaChapterPages
        {
            Id = chapterId,
            Url = url,
            Number = double.TryParse(url.Split('-').Last(), out var n) ? n : 0,
            Title = title,
            Pages = doc
                .DocumentNode
                .SelectNodes("//div[@class=\"container-chapter-reader\"]/img")
                .Select(t => t.GetAttributeValue("src", ""))
                .ToArray(),
        };

        return chapter;
    }

    public Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
    {
        var url = $"{ChapterBaseUri}/manga/{mangaId}/{chapterId}";
        return ChapterPages(url);
    }

    public void FillDetails(Manga manga, HtmlDocument doc)
    {
        var nodes = doc.DocumentNode
            .SelectNodes("//ul[@class='manga-info-text']/li")?
            .ToArray() ?? Array.Empty<HtmlNode>();
        foreach(var node in nodes)
        {
            var text = node.InnerText.HTMLDecode().Trim();
            if (!text.Contains(':')) continue;

            var parts = text.Split(':');
            var key = parts[0].Trim().ToLower();
            var value = string.Join(':', parts.Skip(1)).Trim();

            if (key.Contains("genres"))
                manga.Tags = value.Split(',').Select(t => t.Trim()).ToArray();
        }

        var description = doc.DocumentNode
            .SelectSingleNode("//div[@class='main-wrapper']/div[@class='leftCol']" +
                "/div[@id='contentBox']");
        manga.Description = description?.InnerText.HTMLDecode().Trim() ?? string.Empty;
    }

    public async Task<Manga?> Manga(string id)
    {
        var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}/{id}";
        var doc = await _api.GetHtml(url);
        if (doc == null) return null;

        var manga = new Manga
        {
            Title = doc.DocumentNode.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h1")?.InnerText ?? "",
            Id = id,
            Provider = Provider,
            HomePage = url,
            Cover = doc.DocumentNode.SelectSingleNode("//div[@class=\"manga-info-pic\"]/img")?.GetAttributeValue("src", "") ?? "",
            Referer = MangaBaseUri + "/",
        };

        FillDetails(manga, doc);

        //var desc = doc.DocumentNode.SelectSingleNode("//div[@id='panel-story-info-description']");
        //foreach (var item in desc.ChildNodes.ToArray())
        //{
        //    if (item.Name == "h3") item.Remove();
        //}

        //manga.Description = desc.InnerHtml.Trim().HTMLDecode();

        //var textEntries = doc.DocumentNode.SelectNodes("//div[@class=\"story-info-right\"]/table/tbody/tr");

        //foreach (var tr in textEntries)
        //{
        //    var label = tr.SelectSingleNode("./td[@class=\"table-label\"]")?.InnerText?.Trim().ToLower();

        //    if (!(label?.Contains("genres") ?? false)) 
        //        continue;

        //    var atags = tr.SelectNodes("./td[@class=\"table-value\"]/a").Select(t => t.InnerText).ToArray();
        //    manga.Tags = atags;
        //    break;
        //}

        var chapterEntries = doc.DocumentNode.SelectNodes("//div[@class='chapter-list']/div[@class='row']/span/a");

        int num = chapterEntries.Count;
        foreach (var chapter in chapterEntries)
        {
            var a = chapter;
            var href = a.GetAttributeValue("href", "").TrimStart('/');
            var c = new MangaChapter
            {
                Title = a.InnerText.Trim(),
                Url = href,
                Number = num--,
                Id = href.Split('/').Last(),
            };

            manga.Chapters.Add(c);
        }

        manga.Chapters = manga.Chapters.OrderBy(t => t.Number).ToList();

        return manga;
    }

    public (bool matches, string? part) MatchesProvider(string url)
    {
        var matches = url.ToLower().StartsWith(HomeUrl.ToLower());
        if (!matches) return (false, null);

        var parts = url.Remove(0, HomeUrl.Length).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return (false, null);

        var domain = parts.First();
        if (domain.StartsWith("manga")) return (true, parts.First());

        return (false, null);
    }
}

