namespace CardboardBox.Manga.Providers;

public interface IBattwoSource : IMangaUrlSource { }

public class BattwoSource : IBattwoSource
{
	public string HomeUrl => "https://battwo.com/";

	public string MangaBaseUri => $"{HomeUrl}series/";

	public string ChapterUri => $"{HomeUrl}chapter/";

	public string Provider => "battwo";

	private readonly IApiService _api;

	public BattwoSource(IApiService api)
	{
		_api = api;
	}

	public async Task<MangaChapterPages?> ChapterPages(string url)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var chapterId = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

		throw new NotImplementedException();
	}

	public Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		return ChapterPages(ChapterUri + chapterId);
	}

	public async Task<Manga?> Manga(string id)
	{
		var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}{id}";
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var manga = new Manga
		{
			Title = doc.InnerText("//h3[@class='item-title']/a") ?? "",
			Id = id,
			Provider = Provider,
			HomePage = url,
			Cover = doc.Attribute("//div[@class='row detail-set']/div[@class='col-24 col-sm-8 col-md-6 attr-cover']/img", "src") ?? "",
			Referer = HomeUrl,
			Description = doc.InnerHtml("//div[@id='limit-height-body-summary']/div[@class='limit-html']") ?? "",
			AltTitles = (doc.InnerText("//div[@class='pb-2 alias-set line-b-f']") ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray(),
		};

		var tags = doc.DocumentNode.SelectNodes("//div[@class='attr-item']").Select(t => t.InnerText.Replace("\n", ""));
		foreach(var tag in tags)
		{
			var parts = tag.Split(':');
			if (parts.Length < 2) continue;

			var title = parts.First().ToLower();
			var rest = string.Join(":", parts.Skip(1));
			var split = rest.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();

			var asTags = (string name) => manga.Attributes.AddRange(split.Select(t => new MangaAttribute(name, t)));

			switch(title)
			{
				case "genres": manga.Tags = split; break;
				case "authors": asTags("Author"); break;
				case "artists": asTags("Artist"); break;
				case "original language": asTags("Original Language"); break;
				case "translated language": asTags("Translated Language"); break;
				case "upload status": asTags("Status"); break;
				case "original work": asTags("State"); break;
				case "year of release": asTags("Year"); break;
			}
		}

		manga.Nsfw = manga.Tags.Any(t => new[] { "Mature" }.Contains(t));
		var chaps = doc.DocumentNode.SelectNodes("//div[@class='main']/div/a");
		int num = chaps.Count;
		foreach(var chap in chaps)
		{
			var title = WebUtility.HtmlDecode(chap.InnerText.Replace("\n", ""));
			var uri = $"{HomeUrl}{chap.GetAttributeValue("href", "").Trim('/')}";

			manga.Chapters.Add(new()
			{
				Title = title,
				Url = uri,
				Number = num--,
				Id = uri.Split('/').Last()
			});
		}

		manga.Chapters = manga.Chapters.OrderBy(t => t.Number).ToList();

		return manga;
	}

	public (bool matches, string? part) MatchesProvider(string url)
	{
		if (!url.ToLower().StartsWith(MangaBaseUri)) return (false, null);

		var id = url.Split('/').Last();
		return (true, id);
	}
}
