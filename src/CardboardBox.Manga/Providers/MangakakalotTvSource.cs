namespace CardboardBox.Manga.Providers;

public interface IMangakakalotTvSource : IMangaUrlSource { }

public class MangakakalotTvSource : IMangakakalotTvSource
{
	public string HomeUrl => "https://ww4.mangakakalot.tv/";

	public string ChapterBaseUri => $"{HomeUrl}chapter/";

	public string MangaBaseUri => $"{HomeUrl}manga/";

	public string Provider => "mangakakalot";

	private readonly IApiService _api;

	public MangakakalotTvSource(IApiService api)
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
				.SelectSingleNode("//div[@class=\"rdfa-breadcrumb\"]/div/p")
				.ChildNodes
				.Where(t => t.Name == "span")
				.Last()
				.SelectSingleNode("./a/span")
				.InnerText.Trim(),
			Pages = doc
				.DocumentNode
				.SelectNodes("//div[@class=\"vung-doc\"]/img[@class=\"img-loading\"]")
				.Select(t => t.GetAttributeValue("data-src", ""))
				.ToArray()
		};

		return chapter;
	}

	public Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		var url = $"{ChapterBaseUri}{mangaId}/{chapterId}";
		return ChapterPages(url);
	}

	public async Task<Manga?> Manga(string id)
	{
		var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}{id}";
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var manga = new Manga
		{
			Title = doc.DocumentNode.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h1").InnerText,
			Id = id,
			Provider = Provider,
			HomePage = url,
			Cover = HomeUrl + doc.DocumentNode.SelectSingleNode("//div[@class=\"manga-info-pic\"]/img").GetAttributeValue("src", "").TrimStart('/')
		};

		var desc = doc.DocumentNode.SelectSingleNode("//div[@id='noidungm']");
		foreach (var item in desc.ChildNodes.ToArray())
		{
			if (item.Name == "h2") item.Remove();
		}

		manga.Description = desc.InnerHtml;

		var textEntries = doc.DocumentNode.SelectNodes("//ul[@class=\"manga-info-text\"]/li");

		foreach (var li in textEntries)
		{
			if (!li.InnerText.StartsWith("Genres")) continue;

			var atags = li.ChildNodes.Where(t => t.Name == "a").Select(t => t.InnerText).ToArray();
			manga.Tags = atags;
			break;
		}

		var chapterEntries = doc.DocumentNode.SelectNodes("//div[@class=\"chapter-list\"]/div[@class=\"row\"]");

		int num = chapterEntries.Count;
		foreach (var chapter in chapterEntries)
		{
			var a = chapter.SelectSingleNode("./span/a");
			var href = HomeUrl + a.GetAttributeValue("href", "").TrimStart('/');
			var c = new MangaChapter
			{
				Title = a.InnerText.Trim(),
				Url = href,
				Number = num--,
				Id = href.Split('/').Last()
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
		if (domain.ToLower() == "manga" && parts.Length == 2) return (true, parts.Last());

		if (domain.ToLower() == "chapter" && parts.Length > 1) return (true, parts.Skip(1).First());

		return (false, null);
	}
}
