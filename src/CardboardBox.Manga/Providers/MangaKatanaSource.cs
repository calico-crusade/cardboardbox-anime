namespace CardboardBox.Manga.Providers;

public interface IMangaKatanaSource : IMangaSource { }

public class MangaKatanaSource : IMangaKatanaSource
{
	public string HomeUrl => "https://mangakatana.com/";

	public string MangaBaseUri => "https://mangakatana.com/manga/";

	public string Provider => "mangakatana";

	private readonly IApiService _api;

	public MangaKatanaSource(IApiService api)
	{
		_api = api;
	}

	public async Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		var url = $"{MangaBaseUri}{mangaId}/{chapterId}";
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var regex = new Regex("[^0-9\\.]+");

		var pages = doc.DocumentNode
			.SelectNodes("//script")
			.Select(t => t.InnerHtml)
			.Where(t => t.Contains("thzq=["))
			.SelectMany(t =>
			{
				var sections = t.Split(new[] { "thzq=[" }, StringSplitOptions.RemoveEmptyEntries);
				if (sections.Length < 2) return Array.Empty<string>();

				return sections
					.Last()
					.Split(']')
					.First()
					.Split(new[] { ',', '\'', '\"' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim())
					.ToArray();
			}).ToArray();

		var chapter = new MangaChapterPages
		{
			Id = chapterId,
			Url = url,
			Number = double.TryParse(regex.Replace(url, ""), out var n) ? n : 0,
			Title = doc.InnerText("//li[@class='uk-active uk-visible-large']/span")?.Trim() ?? string.Empty,
			Pages = pages
		};

		return chapter;
	}

	public async Task<Manga?> Manga(string id)
	{
		var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}{id}";
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var manga = new Manga
		{
			Title = doc.Attribute("//meta[@property='og:title']", "content") ?? string.Empty,
			Id = id,
			Provider = Provider,
			HomePage = url,
			Cover = doc.Attribute("//div[@class='d-cell-medium media']/div[@class='cover']/img", "src") ?? string.Empty,
			Description = doc.InnerHtml("//div[@class='summary']/p") ?? string.Empty,
			Referer = HomeUrl
		};

		var meta = doc.DocumentNode.SelectNodes("//ul[@class='meta d-table']/li[@class='d-row-small']");
		foreach(var li in meta)
		{
			var clone = li.Copy();
			var label = clone.InnerText("//div[@class='d-cell-small label']")?.ToLower()?.Trim();

			switch(label)
			{
				case "alt name(s):":
					manga.AltTitles = clone
						.InnerText("//div[@class='alt_name']")?
						.Split(';')
						.Select(t => t.Trim())
						.ToArray() ?? Array.Empty<string>();
					continue;
				case "genres:":
					manga.Tags = clone.SelectNodes("//a[@class='text_0']").Select(t => t.InnerText.Trim()).ToArray();
					continue;
			}
		}

		var chapters = doc.DocumentNode.SelectNodes("//table[@class='uk-table uk-table-striped']/tbody/tr/td/div[@class='chapter']/a");
		var i = chapters.Count;
		foreach(var chap in chapters)
		{
			i--;
			var href = chap.GetAttributeValue("href", "");
			var name = chap.InnerText;

			manga.Chapters.Add(new MangaChapter
			{
				Title = name.Trim(),
				Url = href.Trim(),
				Id = href.Trim('/').Split('/').Last(),
				Number = i
			});
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
		if (domain.ToLower() != "manga") return (false, null);

		if (parts.Length >= 2)
			return (true, parts[1]);

		return (false, null);
	}
}
