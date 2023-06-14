namespace CardboardBox.LightNovel.Core.Sources;

public interface ILntSourceService : ISourceVolumeService { }

public class LntSourceService : ILntSourceService
{
	private readonly IApiService _api;
	private readonly ILogger _logger;

	public string Name => "lnt";

	public string RootUrl => "https://lightnovelstranslations.com";

	public string NovelUrl => RootUrl + "/novel/";

	public LntSourceService(
		IApiService api, 
		ILogger<LntSourceService> logger) 
	{
		_api = api;
		_logger = logger;
	}

	public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
	{
		static (string?, string[]) HandleDescription(HtmlDocument doc)
		{
			var bob = new StringBuilder();
			var desc = doc.DocumentNode.SelectSingleNode("//div[@class='novel_text']");
			if (desc == null) return (null, Array.Empty<string>());

			string[]? titles = null;
			bool onAltTitles = false;
			foreach (var child in desc.ChildNodes)
			{
				if (child.Name == "div")
				{
					onAltTitles = true;
					continue;
				}

				if (child.Name != "p") continue;

				if (!onAltTitles)
				{
					bob.AppendLine($"<p>{child.InnerText}</p>");
					continue;
				}

				titles = child.InnerHtml
					.Split("<br />")
					.Select(t => t.Trim())
					.ToArray();
				break;
			}

			var output = bob.ToString();
			if (string.IsNullOrEmpty(output)) return (null, titles ?? Array.Empty<string>());
			return (output, titles ?? Array.Empty<string>());
		}

		var set = SeriesFromUrl(url);
		if (set == null) return null;

		url = set.Root;
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var title = doc.InnerText("//div[@class='novel_title']/h3");
		if (string.IsNullOrEmpty(title)) return null;

		var (desc, altTitles) = HandleDescription(doc);
		var tags = doc.DocumentNode
			.SelectNodes("//div[@class='novel_tags_item']/span")
			.Select(t => t.InnerText.HTMLDecode())
			.ToArray();

		var details = doc.DocumentNode
			.SelectNodes("//div[@class='novel_detail_info']/ul/li")
			.Select(t =>
			{
				var deets = t.InnerText.HTMLDecode().Split(':');
				var name = deets.First().HTMLDecode();
				var value = string.Join(":", deets.Skip(1)).Trim();
				return (name, value);
			})
			.ToArray();

		var author = details
			.Where(t => t.name == "Author")
			.Select(t => t.value)
			.ToArray();

		var image = doc.Attribute("//div[@class='col-2 novel-image']/img", "data-lazy-srcset")
			?.Split(',')
			.LastOrDefault()
			?.Trim()
			.Split(' ')
			.First();

		var firstChap = doc.Attribute("//div[@class='novel_list_chapter_content']/" +
			"div[@class='accordition_item']/" +
			"div[@class='accordition_item_content']/" +
			"ul/li/a", "href");

		return new TempSeriesInfo(
			title,
			desc,
			author,
			image,
			firstChap,
			tags,
			Array.Empty<string>()
		);
	}

	public LntUrlSet? SeriesFromUrl(string url)
	{
		if (string.IsNullOrEmpty(url)) return null;

		if (!url.StartsWith(NovelUrl, 
			StringComparison.InvariantCultureIgnoreCase)) return null;

		var series = url.Remove(0, NovelUrl.Length).Split('/').First();
		if (string.IsNullOrEmpty(series)) return null;

		var root = NovelUrl + series + "/";

		return new LntUrlSet(root,
			root + "?tab=table_contents",
			root + "?tab=novel_illustrations");
	}

	public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
	{
		static IEnumerable<(string name, string[] images)> Images(HtmlNode accords)
		{
			var clone = accords.Copy();
			var volumes = clone.SelectNodes("//div[@class='accordition_item']");
			if (volumes == null) yield break;

			foreach(var vol in volumes)
			{
				var c = vol.Copy();
				var title = c.InnerText("//h3[@class='accordition_item_title']")?.Trim();
				if (string.IsNullOrEmpty(title)) continue;

				var images = c.SelectNodes("//div[@class='accordition_item_content']/p/img")
					.Select(t => t.GetAttributeValue("src", ""))
					.ToArray();

				yield return (title, images);
			}
		}

		var set = SeriesFromUrl(seriesUrl);
		if (set == null) yield break;

		var url = set.Root;
		var doc = await _api.GetHtml(url);
		if (doc == null) yield break;

		var accordions = doc.DocumentNode.SelectNodes("//div[@class='novel_list_chapter_content']");
		if (accordions == null) yield break;

		var images = Images(accordions[1]).ToArray();
		var table = accordions[0].Copy().SelectNodes("//div[@class='accordition_item']");
		if (table == null) yield break;

		foreach(var vol in table)
		{
			var c = vol.Copy();
			var title = c.InnerText("//h3[@class='accordition_item_title']")?.Trim();
			if (string.IsNullOrEmpty(title)) continue;

			var chapters = c.SelectNodes("//div[@class='accordition_item_content']/ul/li")
				.Select(t =>
				{
					var title = string.Join(": ", t.InnerText.HTMLDecode().Trim().Split(':'));
					var url = t.Copy().Attribute("//a", "href") ?? string.Empty;
					var locked = !t.HasClass("unlock");
					return (locked, new SourceChapterItem
					{
						Title = title,
						Url = url
					});
				})
				.Where(t => !t.locked)
				.Select(t => t.Item2)
				.ToArray();

			var imgs = images.FirstOrDefault(t => t.name == title).images;

			yield return new SourceVolume
			{
				Chapters = chapters,
				Title = title,
				Url = url,
				Forwards = imgs ?? Array.Empty<string>()
			};
		}
	}

	public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var title = doc.InnerText("//div[@class='text_story']/h2");
		if (string.IsNullOrEmpty(title)) return null;

		var next = doc.Attribute("//div[@class=' next_story_btn']/a", "href");

		var locked = doc.DocumentNode.SelectSingleNode("//div[@class='excerpt-text']/div[@class='unlock_chapter text-center']");
		if (locked != null) return null;

		var content = doc.DocumentNode.SelectSingleNode("//div[@class='text_story']");
		var validEls = new[] { "p", "hr" };
		foreach(var child in content.ChildNodes.ToArray())
		{
			if (!validEls.Contains(child.Name))
				content.RemoveChild(child);
		}

		return new SourceChapter
		{
			ChapterTitle = title,
			Content = content.InnerHtml,
			Url = url,
			NextUrl = next ?? "",
			BookTitle = bookTitle,
		};
	}

	public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
	{
		string url = firstUrl,
			seriesUrl = SeriesFromChapter(firstUrl);

		var series = await GetSeriesInfo(seriesUrl);
		if (series == null) yield break;

		while(true)
		{
			var chap = await GetChapter(url, series.Title);
			if (chap == null) yield break;

			yield return chap;

			if (string.IsNullOrEmpty(chap.NextUrl))
				break;

			url = chap.NextUrl;
		}
	}

	public string SeriesFromChapter(string url)
	{
		return SeriesFromUrl(url)?.Root ?? string.Empty;
	}
}

public record class LntUrlSet(string Root, string Volumes, string Images);