namespace CardboardBox.Manga.Providers;

public interface IChapmanganatoSource : IMangaUrlSource { }

public class ChapmanganatoSource : IChapmanganatoSource
{
    public string HomeUrl => "https://chapmanganato.to";

    public string ChapterBaseUri => $"{HomeUrl}";

    public string MangaBaseUri => $"{HomeUrl}/";

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

        var chapter = new MangaChapterPages
        {
            Id = chapterId,
            Url = url,
            Number = double.TryParse(url.Split('-').Last(), out var n) ? n : 0,
            Title = doc
                .DocumentNode
                .SelectSingleNode("//div[@class=\"panel-chapter-info-top\"]/h1")
                .InnerText.Trim(),
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
        var url = $"{ChapterBaseUri}/{mangaId}/{chapterId}";
        return ChapterPages(url);
    }

    public async Task<Manga?> Manga(string id)
    {
        var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}/{id}";
        var doc = await _api.GetHtml(url);
        if (doc == null) return null;

        var manga = new Manga
        {
            Title = doc.DocumentNode.SelectSingleNode("//div[@class=\"story-info-right\"]/h1")?.InnerText ?? "",
            Id = id,
            Provider = Provider,
            HomePage = url,
            Cover = doc.DocumentNode.SelectSingleNode("//span[@class=\"info-image\"]/img[@class=\"img-loading\"]")?.GetAttributeValue("src", "") ?? "",
            Referer = MangaBaseUri + "/",
        };

        var desc = doc.DocumentNode.SelectSingleNode("//div[@id='panel-story-info-description']");
        foreach (var item in desc.ChildNodes.ToArray())
        {
            if (item.Name == "h3") item.Remove();
        }

        manga.Description = desc.InnerHtml.Trim().HTMLDecode();

        var textEntries = doc.DocumentNode.SelectNodes("//div[@class=\"story-info-right\"]/table/tbody/tr");

        foreach (var tr in textEntries)
        {
            var label = tr.SelectSingleNode("./td[@class=\"table-label\"]")?.InnerText?.Trim().ToLower();

            if (!(label?.Contains("genres") ?? false)) 
                continue;

            var atags = tr.SelectNodes("./td[@class=\"table-value\"]/a").Select(t => t.InnerText).ToArray();
            manga.Tags = atags;
            break;
        }

        var chapterEntries = doc.DocumentNode.SelectNodes("//ul[@class=\"row-content-chapter\"]/li[@class=\"a-h\"]/a");

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
        if (domain.StartsWith("manga-")) return (true, parts.First());

        return (false, null);
    }
}

