namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface IFoxaholicSourceService : ISourceVolumeService { }

internal class FoxaholicSource(
	IFlareSolver _flare,
	ISmartReaderService _reader,
	ILogger<FoxaholicSource> _logger) : FlareVolumeSource(_flare, _reader, _logger), IFoxaholicSourceService
{
	private static readonly Regex RootRegex = new("^(https?://[^/]+/novel/[^/]+/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex ChapterNumberRegex = new("chapter(?:[-_/ ]+| )(?<num>\\d+(?:\\.\\d+)*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	public override string Name => "foxaholic";

	public override string RootUrl => "https://www.foxaholic.com";

	public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
	{
		var doc = await Get(url, true);
		if (doc is null) return null;

		var title = doc.InnerText("//h1")?.HTMLDecode()?.Trim()
			?? doc.Attribute("//meta[@property='og:title']", "content")?.HTMLDecode()?.Trim();
		if (string.IsNullOrWhiteSpace(title))
			return null;

		var description = doc.Attribute("//meta[@name='description']", "content")?.HTMLDecode()?.Trim();
		if (string.IsNullOrWhiteSpace(description) || string.Equals(description, title, StringComparison.InvariantCultureIgnoreCase))
		{
			var parts = doc.DocumentNode.SelectNodes("//div[contains(@class,'summary__content')]//p")
				?.Select(t => t.InnerText?.HTMLDecode()?.Trim())
				.Where(t => !string.IsNullOrWhiteSpace(t))
				.Select(t => t!)
				.ToArray() ?? [];

			description = parts.Length > 0
				? string.Join(Environment.NewLine + Environment.NewLine, parts)
				: doc.InnerText("//div[contains(@class,'summary__content')]")?.HTMLDecode()?.Trim();
		}

		var authors = doc.DocumentNode.SelectNodes("//div[contains(@class,'author-content')]//a")
			?.Select(t => t.InnerText?.HTMLDecode()?.Trim())
			.Where(t => !string.IsNullOrWhiteSpace(t))
			.Select(t => t!)
			.ToArray() ?? [];

		var genres = doc.DocumentNode.SelectNodes("//div[contains(@class,'genres-content')]//a")
			?.Select(t => t.InnerText?.HTMLDecode()?.Trim())
			.Where(t => !string.IsNullOrWhiteSpace(t))
			.Select(t => t!)
			.ToArray() ?? [];

		var tags = doc.DocumentNode.SelectNodes("//div[contains(@class,'tags-content')]//a")
			?.Select(t => t.InnerText?.HTMLDecode()?.Trim())
			.Where(t => !string.IsNullOrWhiteSpace(t))
			.Select(t => t!)
			.ToArray() ?? [];

		var image = doc.Attribute("//meta[@property='og:image']", "content")?.Trim()
			?? doc.Attribute("//div[contains(@class,'summary_image')]//img", "src")?.Trim();

		var firstChap = doc.DocumentNode.SelectNodes("//a[@href]")
			?.Select(t => t.GetAttributeValue("href", string.Empty).Trim())
			.LastOrDefault(t => t.Contains("/chapter-", StringComparison.InvariantCultureIgnoreCase) || t.Contains("/chapter/", StringComparison.InvariantCultureIgnoreCase));

		return new TempSeriesInfo(title, description, authors, image, firstChap, genres, tags);
	}

	public override async Task<SourceChapter?> GetChapter(string url, string bookTitle)
	{
		await LimitCheck(CancellationToken.None);

		var doc = await Get(url, true);
		if (doc is null)
		{
			_logger.LogError("Failed to get chapter: {url}", url);
			return null;
		}

		var (title, content) = await _reader.GetCleanArticle(doc, url);
		if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
		{
			var (nt, nc) = BackupParse(doc, url);
			title = nt;
			content = nc;
		}

		if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
		{
			_logger.LogError("Failed to get chapter: {url}", url);
			return null;
		}

		title = MassageTitle(title);
		var nextUrl = NextUrl(doc, url);

		if (!string.IsNullOrEmpty(nextUrl))
			nextUrl = _reader.FixUrl(nextUrl, RootUrl);

		var contentDoc = new HtmlDocument();
		contentDoc.LoadHtml(content);
		var adNodes = contentDoc.DocumentNode.SelectNodes("//p[.//img[@alt='Ads by Pubadx'] or .//img[contains(@src,'close-icon.png')]]")?.ToArray() ?? [];
		foreach (var node in adNodes)
			node.Remove();

		content = contentDoc.DocumentNode.InnerHtml;

		return new SourceChapter(bookTitle, title, content, nextUrl ?? string.Empty, url);
	}

	public override string? NextUrl(HtmlDocument doc, string url)
	{
		return doc.Attribute("//a[@rel='next']", "href")?.Trim()
			?? doc.Attribute("//a[contains(@class,'next') or contains(@class,'chapter-next') or contains(@class,'nav-next')]", "href")?.Trim()
			?? doc.DocumentNode.SelectNodes("//a[@href]")
				?.Select(t => t.GetAttributeValue("href", string.Empty).Trim())
				.FirstOrDefault(t => t.Contains("next", StringComparison.InvariantCultureIgnoreCase) && t.Contains("chapter", StringComparison.InvariantCultureIgnoreCase));
	}

	public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
	{
		var links = doc.DocumentNode.SelectNodes("//a[@href]")
			?.Select(t => new SourceChapterItem
			{
				Title = t.InnerText?.HTMLDecode()?.Trim() ?? string.Empty,
				Url = t.GetAttributeValue("href", string.Empty).Trim()
			})
			.Where(t => !string.IsNullOrWhiteSpace(t.Url))
			.Where(t => t.Url.Contains("/chapter-", StringComparison.InvariantCultureIgnoreCase) || t.Url.Contains("/chapter/", StringComparison.InvariantCultureIgnoreCase))
			.Where(t => !string.IsNullOrWhiteSpace(t.Title))
			.Select(t => new ChapterEntry(
				t,
				_reader.FixUrl(t.Url, RootUrl),
				GetChapterKey(t.Url, t.Title)))
			.OrderBy(t => t, ChapterEntryComparer.Instance)
			.Select(t => new SourceChapterItem
			{
				Title = t.Chapter.Title,
				Url = t.Url
			})
			.ToArray() ?? [];

		if (links.Length == 0)
			yield break;

		yield return new SourceVolume
		{
			Title = "Volume 1",
			Url = url,
			Chapters = links
		};
	}

	public override string SeriesFromChapter(string url)
	{
		var match = RootRegex.Match(url);
		if (!match.Success) return url;

		return match.Groups[1].Value;
	}

	private static ChapterKey? GetChapterKey(string url, string title)
	{
		var keyText = ParseChapterText(title) ?? ParseChapterText(url);
		if (string.IsNullOrWhiteSpace(keyText))
			return null;

		return ChapterKey.TryParse(keyText, out var key) ? key : null;
	}

	private static string? ParseChapterText(string text)
	{
		var match = Regex.Match(text, @"(?<!\d)(\d+(?:\.\d+)*)(?!\d)");
		return match.Success ? match.Groups[1].Value : null;
	}

	private sealed record ChapterEntry(SourceChapterItem Chapter, string Url, ChapterKey? Key);

	private sealed record ChapterKey(int[] Parts) : IComparable<ChapterKey>
	{
		public static bool TryParse(string value, out ChapterKey? key)
		{
			var parts = value.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(t => int.TryParse(t, out var v) ? v : -1)
				.ToArray();

			if (parts.Length == 0 || parts.Any(t => t < 0))
			{
				key = null;
				return false;
			}

			key = new ChapterKey(parts);
			return true;
		}

		public int CompareTo(ChapterKey? other)
		{
			if (other is null) return 1;

			var len = Math.Min(Parts.Length, other.Parts.Length);
			for (var i = 0; i < len; i++)
			{
				var cmp = Parts[i].CompareTo(other.Parts[i]);
				if (cmp != 0) return cmp;
			}

			return Parts.Length.CompareTo(other.Parts.Length);
		}
	}

	private static int CompareKeys(ChapterKey? left, ChapterKey? right)
	{
		if (left is null && right is null) return 0;
		if (left is null) return 1;
		if (right is null) return -1;
		return left.CompareTo(right);
	}

	private sealed class ChapterEntryComparer : IComparer<ChapterEntry>
	{
		public static readonly ChapterEntryComparer Instance = new();

		public int Compare(ChapterEntry? x, ChapterEntry? y)
		{
			if (ReferenceEquals(x, y)) return 0;
			if (x is null) return -1;
			if (y is null) return 1;

			var left = x.Key;
			var right = y.Key;

			var cmp = CompareKeys(left, right);
			if (cmp != 0) return cmp;

			return string.Compare(x.Url, y.Url, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
