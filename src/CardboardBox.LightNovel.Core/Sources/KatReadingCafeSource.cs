using System.Globalization;

namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface IKatReadingCafeSourceService : ISourceVolumeService { }

internal class KatReadingCafeSource(
	IFlareSolver flare,
	ISmartReaderService reader,
	ILogger<KatReadingCafeSource> logger) : FlareVolumeSource(flare, reader, logger), IKatReadingCafeSourceService
{
	private readonly ISmartReaderService _reader = reader;
	private readonly ILogger _logger = logger;
	private readonly Dictionary<string, string> _seriesUrls = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Regex VolumeNumber = new(@"\bVol(?:ume)?\.?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex ChapterNumber = new(@"\bCh(?:apter)?\.?\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public override string Name => "katreadingcafe";
	public override string RootUrl => "https://katreadingcafe.com";
	public override double? ResponseWaitSeconds => 5;

	private static string Class(string name) => $"contains(concat(' ', normalize-space(@class), ' '), ' {name} ')";
	private static string Text(HtmlNode? node) => WebUtility.HtmlDecode(node?.InnerText ?? string.Empty).Trim();

	private string? AbsoluteUrl(string? value, string url)
	{
		value = WebUtility.HtmlDecode(value)?.Trim();
		return !string.IsNullOrEmpty(value) && Uri.TryCreate(new Uri(url), value, out var result)
			&& (result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeHttp)
			? result.AbsoluteUri : null;
	}

	private async Task<string> ResolveSeriesUrl(string url)
	{
		if (new Uri(url).AbsolutePath.StartsWith("/series/", StringComparison.OrdinalIgnoreCase))
			return url;
		if (_seriesUrls.TryGetValue(url, out var series)) return series;

		var doc = await Get(url, true);
		var href = doc.DocumentNode.SelectSingleNode($"//div[{Class("ts-breadcrumb")}]//a[contains(@href, '/series/')]")
			?.GetAttributeValue("href", string.Empty);
		series = AbsoluteUrl(href, url) ?? throw new InvalidOperationException($"Could not find Kat Reading Cafe series for {url}");
		return _seriesUrls[url] = series;
	}

	public override string SeriesFromChapter(string url) => _seriesUrls.GetValueOrDefault(url, url);

	public override async IAsyncEnumerable<SourceVolume> Volumes(string url)
	{
		var seriesUrl = await ResolveSeriesUrl(url);
		await foreach (var volume in base.Volumes(seriesUrl))
			yield return volume;
	}

	public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
	{
		url = await ResolveSeriesUrl(url);
		var doc = await Get(url, true);
		var title = Text(doc.DocumentNode.SelectSingleNode($"//h1[{Class("entry-title")}]"));
		if (string.IsNullOrWhiteSpace(title)) return null;

		var description = Text(doc.DocumentNode.SelectSingleNode($"//div[{Class("sersys")}]"));
		var authors = doc.DocumentNode.SelectNodes($"//div[{Class("serl")}][span[{Class("sername")} and normalize-space(.)='Author']]/span[{Class("serval")}]//a")
			?.Select(Text).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToArray() ?? [];
		var image = AbsoluteUrl(doc.DocumentNode.SelectSingleNode($"//div[{Class("sertothumb")}]//img")
			?.GetAttributeValue("src", string.Empty), url);
		var genres = doc.DocumentNode.SelectNodes($"//div[{Class("sertogenre")}]//a")?.Select(Text).ToArray() ?? [];
		var tags = doc.DocumentNode.SelectNodes($"//div[{Class("sertags")}]//a")?.Select(Text).ToArray() ?? [];
		var volumes = await GetVolumes(url, doc);

		return new TempSeriesInfo(title, description, authors, image,
			volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url, genres, tags);
	}

	private static decimal Number(Regex regex, string value, decimal fallback)
	{
		var match = regex.Match(value);
		return match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint,
			CultureInfo.InvariantCulture, out var number) ? number : fallback;
	}

	public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
	{
		var lists = doc.DocumentNode.SelectNodes($"//div[{Class("eplister")}]//ul/li/a[@href]")?.ToArray() ?? [];
		var entries = lists.Select((node, index) =>
		{
			var label = Text(node.SelectSingleNode($".//*[{Class("epl-num")}]"));
			var title = Text(node.SelectSingleNode($".//*[{Class("epl-title")}]"));
			var group = Text(node.SelectSingleNode($"ancestor::div[{Class("katoc-group")}][1]/*[{Class("ts-chl-collapsible")}]"));
			var chapterUrl = AbsoluteUrl(node.GetAttributeValue("href", string.Empty), url);
			return new
			{
				Volume = Number(VolumeNumber, label, Number(VolumeNumber, group, 1)),
				Chapter = Number(ChapterNumber, label, index),
				Item = new SourceChapterItem { Title = string.IsNullOrEmpty(title) ? label : $"{label}: {title}", Url = chapterUrl ?? string.Empty }
			};
		}).Where(t => !string.IsNullOrEmpty(t.Item.Url) && !string.IsNullOrEmpty(t.Item.Title))
			.DistinctBy(t => t.Item.Url, StringComparer.OrdinalIgnoreCase)
			.GroupBy(t => t.Volume).OrderBy(t => t.Key);

		foreach (var volume in entries)
		{
			var chapters = volume.OrderBy(t => t.Chapter).Select(t => t.Item).ToArray();
			foreach (var chapter in chapters) _seriesUrls[chapter.Url] = url.TrimEnd('/') + "/";
			yield return new SourceVolume
			{
				Title = "Volume " + volume.Key.ToString(CultureInfo.InvariantCulture),
				Url = url,
				Chapters = chapters
			};
		}

		await Task.CompletedTask;
	}

	public override string? NextUrl(HtmlDocument doc, string url)
	{
		var href = doc.DocumentNode.SelectSingleNode($"//div[{Class("naveps")} or {Class("bottomnav")}]//a[@rel='next' and not(@aria-disabled='true')]")
			?.GetAttributeValue("href", string.Empty);
		var next = AbsoluteUrl(href, url);
		return next == url || href?.Trim() == "#" ? null : next;
	}

	public override async Task<SourceChapter?> GetChapter(string url, string bookTitle)
	{
		await LimitCheck(CancellationToken.None);
		var doc = await Get(url, true);
		var title = Text(doc.DocumentNode.SelectSingleNode($"//h1[{Class("entry-title")}]"));
		var content = doc.DocumentNode.SelectSingleNode($"//div[{Class("epcontent")} and {Class("entry-content")}]")?.CloneNode(true);
		if (string.IsNullOrWhiteSpace(title) || content is null)
		{
			_logger.LogError("Failed to parse Kat Reading Cafe chapter: {url}", url);
			return null;
		}

		// Keep the story and translator notes, excluding reader controls and injected notices.
		var unwanted = content.SelectNodes($".//script|.//style|.//noscript|.//button|.//*[@hidden or @aria-hidden='true' or {Class("krc-wm")} or {Class("ckb-announcement")} or {Class("ckb-wrap")}]")?.ToArray() ?? [];
		foreach (var node in unwanted) node.Remove();
		var cleanDoc = new HtmlDocument();
		cleanDoc.LoadHtml(content.InnerHtml);
		HtmlStyleUtility.InlineStylesAndRemoveHidden(cleanDoc);
		foreach (var image in cleanDoc.DocumentNode.SelectNodes("//img[@src]")?.ToArray() ?? [])
		{
			var src = AbsoluteUrl(image.GetAttributeValue("src", string.Empty), url);
			if (src is not null) image.SetAttributeValue("src", src);
		}
		var html = _reader.CleanseHtml(cleanDoc.DocumentNode.InnerHtml, RootUrl);
		if (string.IsNullOrWhiteSpace(html)) return null;

		return new SourceChapter(bookTitle, title, html, NextUrl(doc, url) ?? string.Empty, url);
	}
}
