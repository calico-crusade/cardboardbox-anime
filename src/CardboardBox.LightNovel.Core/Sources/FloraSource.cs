namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface IFloraSourceService : ISourceVolumeService { }

internal class FloraSource(
	IFlareSolver _flare,
	ISmartReaderService _reader,
	ILogger<FloraSource> _logger) : FlareVolumeSource(_flare, _reader, _logger), IFloraSourceService
{
	private readonly ISmartReaderService _reader = _reader;

	private static readonly Regex SeriesRegex = new(
		"^(?<series>https?://[^/]+/[^/]+/)",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	public override string Name => "floratl";

	public override string RootUrl => "https://floratl.com";

	public override double? ResponseWaitSeconds => 5;

	public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
	{
		var doc = await Get(url, true);

		var title = PageTitle(doc);
		if (string.IsNullOrWhiteSpace(title)) return null;

		var description = doc.InnerText("//details[summary[contains(normalize-space(.),'Summary')]]//p")
			?.HTMLDecode()?.Trim();
		var author = doc.DocumentNode.SelectNodes("//li")
			?.Select(t => t.InnerText.HTMLDecode().Trim())
			.FirstOrDefault(t => t.StartsWith("Author:", StringComparison.InvariantCultureIgnoreCase))
			?.Split(':', 2)[1].Trim();
		var authors = string.IsNullOrWhiteSpace(author) ? Array.Empty<string>() : new[] { author };
		var image = doc.Attribute("//figure[contains(@class,'wp-block-image')]//img", "src")?.Trim()
			?? doc.Attribute("//meta[@property='og:image']", "content")?.Trim();
		var firstChapter = ChapterLinks(doc, url).FirstOrDefault()?.Url;

		return new TempSeriesInfo(title, description, authors, image, firstChapter, [], []);
	}

	public override async Task<SourceChapter?> GetChapter(string url, string bookTitle)
	{
		await LimitCheck(CancellationToken.None);

		var doc = await Get(url, true);
		var title = PageTitle(doc);
		var content = doc.DocumentNode.SelectSingleNode(
			"//div[contains(@class,'entry-content') and contains(@class,'guten-post-content')]" +
			"//div[contains(@class,'text-content-inner')]")?.InnerHtml?.Trim();

		if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
		{
			_logger.LogError("Failed to parse chapter: {url}", url);
			return null;
		}

		var nextUrl = NextUrl(doc, url);
		if (!string.IsNullOrWhiteSpace(nextUrl))
			nextUrl = _reader.FixUrl(nextUrl, RootUrl);

		return new SourceChapter(bookTitle, title, content, nextUrl ?? string.Empty, url);
	}

	public override string? NextUrl(HtmlDocument doc, string url)
	{
		return doc.Attribute("//div[contains(@class,'post-navigation-link-next')]//a[@rel='next']", "href")?.Trim();
	}

	public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
	{
		var chapters = ChapterLinks(doc, url).ToArray();
		_logger.LogInformation("Found {count} FloraTL chapter links on {url}", chapters.Length, url);
		if (chapters.Length == 0) yield break;

		yield return new SourceVolume
		{
			Title = "Volume 1",
			Url = url,
			Chapters = chapters
		};

		await Task.CompletedTask;
	}

	public override string SeriesFromChapter(string url)
	{
		var match = SeriesRegex.Match(url);
		return match.Success ? match.Groups["series"].Value : url;
	}

	private IEnumerable<SourceChapterItem> ChapterLinks(HtmlDocument doc, string seriesUrl)
	{
		// The tab wrapper is emitted by a Gutenberg plugin and its generated classes
		// can differ between the normal response and the FlareSolver response. The
		// URL pattern is specific enough to safely find chapters across the page.
		return doc.DocumentNode.SelectNodes("//a[@href]")
			?.Select(t => (Node: t, ListItem: t.Ancestors("li").FirstOrDefault()))
			.Select(t => new SourceChapterItem
			{
				Title = (t.ListItem?.InnerText ?? t.Node.InnerText).HTMLDecode().Trim(),
				Url = NormalizeUrl(t.Node.GetAttributeValue("href", string.Empty))
			})
			.Where(t => !string.IsNullOrWhiteSpace(t.Title) && IsChapterUrl(t.Url, seriesUrl))
			.DistinctBy(t => t.Url, StringComparer.InvariantCultureIgnoreCase)
			?? [];
	}

	private string NormalizeUrl(string url)
	{
		url = url.HTMLDecode().Trim();
		if (url.StartsWith("//")) url = $"https:{url}";
		return Uri.TryCreate(new Uri($"{RootUrl}/"), url, out var result)
			? result.AbsoluteUri
			: url;
	}

	private static bool IsChapterUrl(string url, string seriesUrl)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out var chapter) ||
			!Uri.TryCreate(seriesUrl, UriKind.Absolute, out var series) ||
			!NormalizeHost(chapter.Host).Equals(
				NormalizeHost(series.Host), StringComparison.InvariantCultureIgnoreCase))
			return false;

		var seriesPath = series.AbsolutePath.TrimEnd('/') + "/";
		if (!chapter.AbsolutePath.StartsWith(seriesPath, StringComparison.InvariantCultureIgnoreCase))
			return false;

		// Chapter and side-story pages are direct children of the novel page.
		var childPath = chapter.AbsolutePath[seriesPath.Length..].Trim('/');
		return childPath.Length > 0 && !childPath.Contains('/');
	}

	private static string NormalizeHost(string host) =>
		host.StartsWith("www.", StringComparison.InvariantCultureIgnoreCase) ? host[4..] : host;

	private static string? PageTitle(HtmlDocument doc)
	{
		// Gutenberg's generated wrapper classes are not stable between cached,
		// browser, and FlareSolver responses. Prefer the post heading, but fall
		// back to any non-site heading and finally the HTML document title.
		var title = doc.InnerText("//div[contains(@class,'guten-post-title')]//h1")
			?.HTMLDecode()?.Trim();
		if (!string.IsNullOrWhiteSpace(title)) return title;

		title = doc.DocumentNode.SelectNodes("//main//h1 | //article//h1 | //h1")
			?.Select(t => t.InnerText.HTMLDecode().Trim())
			.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t) &&
				!t.Equals("FloraTL", StringComparison.InvariantCultureIgnoreCase));
		if (!string.IsNullOrWhiteSpace(title)) return title;

		title = doc.InnerText("//title")?.HTMLDecode()?.Trim()
			?? doc.Attribute("//meta[@property='og:title']", "content")?.HTMLDecode()?.Trim();
		if (string.IsNullOrWhiteSpace(title)) return null;

		var siteSeparator = title.IndexOf(" – ", StringComparison.InvariantCulture);
		return siteSeparator < 0 ? title : title[..siteSeparator].Trim();
	}
}
