namespace CardboardBox.Manga.Providers
{
	public interface IMangakakalotComSource : IMangaUrlSource { }

	public class MangakakalotComSource : IMangakakalotComSource
	{
		public string HomeUrl => "https://mangakakalot.com/";

		public string ChapterBaseUri => $"{HomeUrl}chapter/";

		public string MangaBaseUri => $"{HomeUrl}read-";

		public string Provider => "mangakakalot-com";

		private readonly IApiService _api;

		public MangakakalotComSource(IApiService api)
		{
			_api = api;
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
				Cover = doc.DocumentNode.SelectSingleNode("//div[@class=\"manga-info-pic\"]/img").GetAttributeValue("src", ""),
				Referer = HomeUrl
			};

			var desc = doc.DocumentNode.SelectSingleNode("//div[@id='noidungm']");
			foreach(var item in desc.ChildNodes.ToArray())
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
			if (parts.Length == 1 && domain.StartsWith("read")) return (true, domain.Remove(0, 5));

			return (false, null);
		}
	}
}
