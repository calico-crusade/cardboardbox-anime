namespace CardboardBox.Manga.Providers;

public interface INhentaiSource : IMangaSource { }

public class NhentaiSource : INhentaiSource
{
	private const string DEFAULT_CHAPTER_TITLE = "Chapter 1";
	public string HomeUrl => "https://nhentai.to/";

	public string MangaBaseUri => $"{HomeUrl}g/";

	public string Provider => "nhentai";

	private readonly IApiService _api;

	public NhentaiSource(IApiService api)
	{
		_api = api;
	}

	public string FixPreview(string url)
	{
		var parts = url.Split('/');
		var fname = parts.Last();

		var ext = Path.GetExtension(fname);
		var fwext = Path.GetFileNameWithoutExtension(fname);
		if (fwext.EndsWith("t"))
			fwext = fwext.Substring(0, fwext.Length - 1);

		return string.Join('/', parts.SkipLast().Append($"{fwext}{ext}"));
	}

	public async Task<MangaChapterPages?> ChapterPages(string id, string _)
	{
		var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}{id}";
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		return new MangaChapterPages
		{
			Id = id,
			Title = DEFAULT_CHAPTER_TITLE,
			Number = 1,
			Url = url,
			Volume = 1,
			Pages = doc.DocumentNode
				.SelectNodes("//div[@class='container']/div[@class='thumb-container']/a/img")
				.Select(t => FixPreview(t.GetAttributeValue("data-src", "").Trim()))
				.ToArray()
		};
	}

	public async Task<Manga?> Manga(string id)
	{
		var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}{id}";
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var manga = new Manga
		{
			Title = doc.InnerText("//div[@id='info']/h1")?.Trim() ?? "",
			Id = id,
			Referer = HomeUrl,
			Provider = Provider,
			HomePage = url,
			Cover = doc.Attribute("//div[@id='cover']/a/img", "src") ?? "",
			Nsfw = true,
			Tags = doc.DocumentNode
					  .SelectNodes("//span[@class='tags']/a[contains(@href, '/tag')]/span[@class='name']")
					  .Select(t => t.InnerText.Trim())
					  .ToArray()
		};

		manga.Chapters.Add(new MangaChapter
		{
			Id = id,
			Title = DEFAULT_CHAPTER_TITLE,
			Number = 1,
			Url = url,
			Volume = 1
		});

		return manga;
	}

	public (bool matches, string? part) MatchesProvider(string url)
	{
		var matches = url.ToLower().StartsWith(HomeUrl.ToLower());
		if (!matches) return (false, null);

		var parts = url.Remove(0, HomeUrl.Length).Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0) return (false, null);

		var domain = parts.First();
		if (domain.ToLower() != "g") return (false, null);

		if (parts.Length >= 2)
			return (true, parts[1]);

		return (false, null);
	}
}
