namespace CardboardBox.LightNovel.Core.Sources;

public interface ILntSourceService : ISourceVolumeService { }

public class LntSourceService : ILntSourceService
{
	private readonly IApiService _api;
	private readonly ILogger _logger;
	private readonly IConfiguration _config;

	public string Name => "lnt";

	public string RootUrl => "https://lightnovelstranslations.com";

	public string NovelUrl => RootUrl + "/novel/";

	public string? AuthValue => null;//_config["NovelSources:LntAuth"];

	public LntSourceService(
		IApiService api, 
		ILogger<LntSourceService> logger,
		IConfiguration config) 
	{
		_api = api;
		_logger = logger;
		_config = config;
	}

	public async Task<HtmlDocument> GetWithAuth(string url)
	{
		var proxied = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
		{
			["https://lightnovelstranslations.com/novel/level-1-strongest-sage/?tab=table_contents"] = "ProxiedPages\\l1ss.html",
            ["https://lightnovelstranslations.com/novel/level-1-strongest-sage?tab=table_contents"] = "ProxiedPages\\l1ss.html"
        };

		if (proxied.TryGetValue(url, out var path) && File.Exists(path))
		{
			_logger.LogInformation("Using proxied page for {Url} from {Path}", url, path);
			var data = await File.ReadAllTextAsync(path);
			var doc = new HtmlDocument();
			doc.LoadHtml(data);
			return doc;
        }

        var authValue = AuthValue;
		return await _api.GetHtml(url, c =>
		{
			if (!string.IsNullOrEmpty(authValue))
				c.Headers.Add("Cookie", authValue);

			c.Headers.Remove("user-agent");
			c.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/114.0");
		});
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
		var doc = await GetWithAuth(url);
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

		var cover = WithClass(doc.DocumentNode, "novel-image").SelectMany(n => n.Descendants("img")).FirstOrDefault();
		var image = (cover?.GetAttributeValue("data-lazy-srcset", null) ?? cover?.GetAttributeValue("srcset", null))
			?.Split(',')
			.LastOrDefault()
			?.Trim()
			.Split(' ')
			.First() ?? cover?.GetAttributeValue("src", null);

		var firstChap = doc.DocumentNode.SelectSingleNode("//a[@id='lnt-reading-button']")?.GetAttributeValue("href", null)
			?? doc.Attribute("//div[@class='novel_list_chapter_content']/" +
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

		if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
		var series = uri.AbsolutePath.Remove(0, "/novel/".Length).Split('/').First();
		if (string.IsNullOrEmpty(series)) return null;

		var root = NovelUrl + series + "/";

		return new LntUrlSet(root,
			root + "?tab=table_contents",
			root + "?tab=novel_illustrations");
	}

	public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
	{
		var set = SeriesFromUrl(seriesUrl);
		if (set == null) yield break;

		var doc = await GetWithAuth(set.Root);
		var modernVolumes = WithClass(doc.DocumentNode, "lnt-volume").ToArray();
		if (modernVolumes.Length > 0)
		{
			// The new TOC only renders the selected volume and 50 chapters per page.
			// Follow its normal HTML links so this also works without JavaScript.
			var galleries = await GetWithAuth(set.Images);
			foreach (var volume in modernVolumes)
			{
				var heading = WithClass(volume, "lnt-volume-link").FirstOrDefault();
				if (heading == null) continue;
				var titleNode = heading.CloneNode(true);
				foreach (var count in titleNode.Descendants("span").ToArray()) count.Remove();
				var title = titleNode.InnerText.HTMLDecode().Trim();
				var chapters = new List<SourceChapterItem>();
				var seenChapters = new HashSet<string>(StringComparer.Ordinal);
				var seenPages = new HashSet<string>(StringComparer.Ordinal);
				var page = WithClass(volume, "lnt-chapter-page").FirstOrDefault();
				string? pageUrl = AbsoluteUrl(set.Root, heading.GetAttributeValue("href", ""));
				while (pageUrl != null && seenPages.Add(pageUrl))
				{
					if (page == null)
					{
						var pageDoc = await GetWithAuth(pageUrl);
						var current = WithClass(pageDoc.DocumentNode, "lnt-volume")
							.First(v => v.GetAttributeValue("data-volume", "") == volume.GetAttributeValue("data-volume", ""));
						page = WithClass(current, "lnt-chapter-page").FirstOrDefault()
							?? throw new InvalidOperationException($"Missing LNT chapter list at {pageUrl}");
					}
					chapters.AddRange(ReadChapters(page, set.Root).Where(c => seenChapters.Add(c.Url)));
					var pageNumber = page.GetAttributeValue("data-page", 1);
					pageUrl = WithClass(page, "lnt-page-link")
						.Select(a => AbsoluteUrl(set.Root, a.GetAttributeValue("href", "")))
						.FirstOrDefault(link => link != null &&
							int.TryParse(Regex.Match(link, @"[?&]lnt_page=(\d+)").Groups[1].Value, out var number) &&
							number == pageNumber + 1);
					page = null;
				}

				var gallery = WithClass(galleries.DocumentNode, "lnt-gallery-content")
					.FirstOrDefault(g => g.SelectSingleNode("./h3")?.InnerText.HTMLDecode().Trim() == title);
				if (gallery == null)
				{
					var link = WithClass(galleries.DocumentNode, "lnt-gallery-nav")
						.SelectMany(n => n.Descendants("a"))
						.FirstOrDefault(a => a.InnerText.HTMLDecode().Trim() == title);
					var galleryUrl = AbsoluteUrl(set.Root, link?.GetAttributeValue("href", ""));
					if (galleryUrl != null)
						gallery = WithClass((await GetWithAuth(galleryUrl)).DocumentNode, "lnt-gallery-content").FirstOrDefault();
				}
				yield return new SourceVolume
				{
					Title = title,
					Url = set.Root,
					Chapters = chapters.ToArray(),
					Forwards = ReadImages(gallery, set.Root)
				};
			}
			yield break;
		}

		// Keep support for older pages, including the locally proxied catalog.
		var accordions = WithClass(doc.DocumentNode, "novel_list_chapter_content").ToArray();
		if (accordions.Length == 0) yield break;
		var images = accordions.Skip(1).SelectMany(n => WithClass(n, "accordition_item")).ToArray();
		foreach (var volume in WithClass(accordions[0], "accordition_item"))
		{
			var title = WithClass(volume, "accordition_item_title").FirstOrDefault()?.InnerText.HTMLDecode().Trim();
			if (string.IsNullOrEmpty(title)) continue;
			var gallery = images.FirstOrDefault(n =>
				WithClass(n, "accordition_item_title").FirstOrDefault()?.InnerText.HTMLDecode().Trim() == title);
			yield return new SourceVolume
			{
				Chapters = ReadChapters(volume, set.Root).ToArray(),
				Title = title,
				Url = set.Root,
				Forwards = ReadImages(gallery, set.Root)
			};
		}
	}

	private static IEnumerable<HtmlNode> WithClass(HtmlNode node, string name) =>
		node.Descendants().Where(n => n.HasClass(name));

	private static string? AbsoluteUrl(string root, string? href)
	{
		if (string.IsNullOrWhiteSpace(href)) return null;
		href = WebUtility.HtmlDecode(href).Trim();
		if (!Uri.TryCreate(new Uri(root), href, out var uri) ||
			(uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) return null;
		// Absolute source URLs are database keys; preserve their percent-escape casing.
		return Uri.IsWellFormedUriString(href, UriKind.Absolute) ? href : uri.AbsoluteUri;
	}

	private static IEnumerable<SourceChapterItem> ReadChapters(HtmlNode node, string root)
	{
		foreach (var row in node.Descendants("li").Where(n => n.HasClass("unlock") && !n.HasClass("lock")))
		{
			var url = AbsoluteUrl(root, row.SelectSingleNode("./a")?.GetAttributeValue("href", ""));
			if (url == null) continue;
			yield return new SourceChapterItem
			{
				// Preserve the existing title format used to identify stored chapters.
				Title = string.Join(": ", row.InnerText.HTMLDecode().Trim().Split(':')),
				Url = url
			};
		}
	}

	private static string[] ReadImages(HtmlNode? node, string root) =>
		node?.Descendants("img").Select(n => AbsoluteUrl(root, n.GetAttributeValue("src", "")))
			.Where(url => url != null).Cast<string>().Distinct().ToArray() ?? [];

	public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
	{
		var doc = await GetWithAuth(url);
		if (doc == null) return null;

		var content = WithClass(doc.DocumentNode, "text_story").FirstOrDefault();
		var title = content?.SelectSingleNode("./h2")?.InnerText.HTMLDecode().Trim();
		if (string.IsNullOrEmpty(title)) return null;

		var next = AbsoluteUrl(url, WithClass(doc.DocumentNode, "next_story_btn")
			.SelectMany(n => n.Descendants("a")).FirstOrDefault()?.GetAttributeValue("href", ""));

		if (WithClass(doc.DocumentNode, "unlock_chapter").Any()) return null;

		var validEls = new[] { "p", "hr", "img" };
		foreach(var child in content!.ChildNodes.ToArray())
		{
			if (!validEls.Contains(child.Name) || child.InnerHtml == "&nbsp;")
			{
				content.RemoveChild(child);
				continue;
			}

			if (child.InnerHtml.ToLower().Contains("<img"))
				child.CleanupNode();
		}

		while (content.FirstChild?.Name == "hr") content.RemoveChild(content.FirstChild);
		while (content.LastChild?.Name == "hr") content.RemoveChild(content.LastChild);
		if (string.IsNullOrWhiteSpace(content.InnerText.HTMLDecode()) && !content.Descendants("img").Any()) return null;

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
