namespace CardboardBox.Manga.Providers;

public abstract class MangakakalotComBase : IMangaUrlSource
{
	public virtual string HomeUrl => "https://mangakakalot.com/";

	public abstract string MangaBaseUri { get; }

	public abstract string Provider { get; }

	private readonly IApiService _api;
	private readonly ILogger _logger;

	public MangakakalotComBase(IApiService api, ILogger<MangakakalotComBase> logger)
	{
		_api = api;
		_logger = logger;
	}

	public Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		throw new NotImplementedException();
	}

	public async Task<MangaChapterPages?> ChapterPages(string url)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var chapterId = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();

		var title = doc
			.DocumentNode
			.SelectSingleNode("//div[@class='breadcrumb breadcrumbs bred_doc']/p")
			.ChildNodes
			.Where(t => t.Name == "span")
			.Last()
			.SelectSingleNode("./a/span")
			.InnerText.Trim();

		var pages = doc
			.DocumentNode
			.SelectNodes("//div[@class='container-chapter-reader']/img")
			.Select(t => t.GetAttributeValue("src", ""))
			.ToArray();

		var chapter = new MangaChapterPages
		{
			Id = chapterId,
			Url = url,
			Number = double.TryParse(url.Split('_', '-').Last(), out var n) ? n : 0,
			Title = title,
			Pages = pages
		};

		return chapter;
	}

	public virtual async Task<Manga?> Manga(string id)
	{
		try
		{
			var url = id.ToLower().StartsWith("http") ? id : $"{MangaBaseUri}{id}";
			var doc = await _api.GetHtml(url);
			if (doc == null) return null;

			var title = doc.DocumentNode.SelectSingleNode("//ul[@class=\"manga-info-text\"]/li/h1").InnerText;
			var cover = doc.DocumentNode.SelectSingleNode("//div[@class=\"manga-info-pic\"]/img").GetAttributeValue("src", "");

			var manga = new Manga
			{
				Title = title,
				Id = id,
				Provider = Provider,
				HomePage = url,
				Cover = cover,
				Referer = HomeUrl
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
				var href = a.GetAttributeValue("href", "").TrimStart('/');
				if (!href.StartsWith("http")) href = HomeUrl + "/" + href;

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
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get manga");
            return null;
		}
	}

	public virtual (bool matches, string? part) MatchesProvider(string url)
	{
		url = url.ToLower().Trim();
		if (!url.StartsWith(MangaBaseUri)) return (false, null);

		var domain = url.Remove(0, MangaBaseUri.Length).Trim('/', '.', '-', ' ');
		return (true, domain);
	}
}

public interface IMangakakalotComSource : IMangaUrlSource { }

public class MangakakalotComSource : MangakakalotComBase, IMangakakalotComSource
{
	public override string MangaBaseUri => $"{HomeUrl}read-";

	public override string Provider => "mangakakalot-com";

	public MangakakalotComSource(IApiService api, ILogger<MangakakalotComSource> logger) : base(api, logger) { }
}

public interface IMangakakalotComAltSource : IMangaUrlSource { }

public class MangakakalotComAltSource : MangakakalotComBase, IMangakakalotComAltSource
{
	public override string Provider => "mangakakalot-com-alt";

	public override string MangaBaseUri => $"{HomeUrl}manga/";

	public MangakakalotComAltSource(IApiService api, ILogger<MangakakalotComSource> logger) : base(api, logger) { }
}
