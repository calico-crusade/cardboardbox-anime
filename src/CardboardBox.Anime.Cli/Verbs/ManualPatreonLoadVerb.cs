using CardboardBox.Extensions;
using CommandLine;
using System.Runtime.CompilerServices;

namespace CardboardBox.Anime.Cli.Verbs;

using LightNovel.Core;
using LightNovel.Core.Sources.Utilities;

[Verb("patreon", HelpText = "Load novels from patreon sources (RR specifically)")]
public class ManualPatreonLoadOptions
{
	[Option('d', "directory", HelpText = "The directory to load from")]
	public string? Directory { get; set; }
}

internal partial class ManualPatreonLoadVerb(
	ILnDbService _db,
	ISmartReaderService _smart,
	ILogger<ManualPatreonLoadVerb> logger) : BooleanVerb<ManualPatreonLoadOptions>(logger)
{
	public static IEnumerable<SeriesUnnest> Unnest(FullScaffold scaffold)
	{
		var series = scaffold.Series;
		foreach (var book in scaffold.Books)
			foreach (var chap in book.Chapters)
				foreach (var page in chap.Pages)
					yield return new SeriesUnnest(series, book.Book, chap.Chapter, page.Page, page.Map);
	}

	public static string GenerateUrl(string baseUrl, string title)
	{
		var cleanTitle = NonAlphaNumeric().Replace(title, "").Replace(" ", "-");
		return $"{baseUrl.TrimEnd('/')}/{cleanTitle}".ToLower();
	}

	public async IAsyncEnumerable<ChapterFile> LoadChapters(string directory, string baseUrl,
		[EnumeratorCancellation] CancellationToken token)
	{
		static void FixPs(HtmlNode node)
		{
			if (node == null) return;

			if (node.Name.EqualsIc("p"))
			{
				var content = node.InnerHtml.Replace("\r", " ").Replace("\n", " ");
				while (content.Contains("  "))
					content = content.Replace("  ", " ");

				node.InnerHtml = content;
			}

			if (node.ChildNodes == null || node.ChildNodes.Count == 0) return;

			foreach (var child in node.ChildNodes?.ToArray() ?? [])
				FixPs(child);
		}

		var files = Directory.GetFiles(directory, "*.html")
			.OrderBy(t => int.TryParse(Path.GetFileNameWithoutExtension(t).TrimEnd('.'), out var val) ? val : int.MaxValue);
		foreach(var file in files)
		{
			var text = await File.ReadAllTextAsync(file, token);
			var title = H1Regex().Match(text).Groups[1].Value;
			var url = GenerateUrl(baseUrl, title);
			var doc = new HtmlDocument();
			doc.LoadHtml(text);
			FixPs(doc.DocumentNode);
			text = doc.DocumentNode.InnerHtml;
			var result = _smart.CleanseHtml(text, url);
			yield return new(title, url, result);
		}
	}

	public async Task<bool> ProcessSeries(int seriesId, string directory, CancellationToken token)
	{
		var series = await _db.Series.Scaffold(seriesId);
		if (series is null)
		{
			_logger.LogError("Series with ID {SeriesId} not found in the database.", seriesId);
			return false;
		}

		var baseUrl = $"{series.Series.Url.TrimEnd('/')}/chapter/999999";

		if (!Directory.Exists(directory))
		{
			_logger.LogError("The specified directory does not exist: {Directory}", directory);
			return false;
		}

		var unnest = Unnest(series).ToArray();
		var urls = unnest.Select(t => t.Page.Url).Distinct().ToHashSet();
		var lastBook = series.Books.OrderByDescending(t => t.Book.Ordinal).FirstOrDefault()?.Book;
		if (lastBook is null)
		{
			_logger.LogError("No books found for series with ID {SeriesId}. Cannot determine the next book ordinal.", seriesId);
			return false;
		}

		var chapOrdinal = unnest.Where(t => t.Book.Id == lastBook.Id)
			.OrderByDescending(t => t.Chapter.Ordinal)
			.FirstOrDefault()?.Chapter?.Ordinal ?? 0;

		var pageOrdinal = unnest.OrderByDescending(t => t.Page.Ordinal)
			.FirstOrDefault()?.Page?.Ordinal ?? 0;

		Page? lastPage = null;

		int loaded = 0;

		var chapters = LoadChapters(directory, baseUrl, token);
		await foreach (var chapter in chapters)
		{
			if (urls.Contains(chapter.Url))
			{
				_logger.LogWarning("Chapter with URL {Url} already exists in the database. Skipping.", chapter.Url);
				continue;
			}

			if (lastPage is not null)
			{
				lastPage.NextUrl = chapter.Url;
				await _db.Pages.Update(lastPage);
			}

			chapOrdinal++;
			pageOrdinal++;
			var chap = new Chapter
			{
				HashId = $"{chapter.Title}-{chapOrdinal}".MD5Hash(),
				Title = chapter.Title,
				Ordinal = chapOrdinal,
				BookId = lastBook.Id,
			};
			chap.Id = await _db.Chapters.Upsert(chap);

			var page = new Page
			{
				HashId = chapter.Url.MD5Hash(),
				Title = chapter.Title,
				Ordinal = pageOrdinal,
				SeriesId = seriesId,
				Url = chapter.Url,
				Content = chapter.Content,
				Mimetype = "application/html"
			};
			page.Id = await _db.Pages.Upsert(page);
			lastPage = page;

			var cp = new ChapterPage
			{
				ChapterId = chap.Id,
				PageId = page.Id,
				Ordinal = 0
			};
			cp.Id = await _db.ChapterPages.Upsert(cp);
			urls.Add(chapter.Url);
			loaded++;

			_logger.LogInformation("Loaded new chapter: [{SeriesId}::{Series}] >> {Title} (URL: {Url}). Total loaded: {loaded}", 
				seriesId, series.Series.Title, chapter.Title, chapter.Url, loaded);
		}

		_logger.LogInformation("Finished loading chapters. Total new chapters loaded: {Loaded}", loaded);

		return true;
	}

	public static string? DetermineDirectory(string? directory)
	{
		if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
			return directory;

		var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "novels", "load");
		if (Directory.Exists(baseDir))
			return baseDir;

		return null;
	}

	public override async Task<bool> Execute(ManualPatreonLoadOptions options, CancellationToken token)
	{
		var directory = DetermineDirectory(options?.Directory);
		if (directory is null)
		{
			_logger.LogError("Invalid options provided. Directory: {Directory}", 
				 options?.Directory);
			return false;
		}

		var directories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
		foreach(var dir in directories)
		{
			var folderName = dir.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
			if (string.IsNullOrEmpty(folderName))
			{
				_logger.LogInformation("Skipping directory {Directory} as it does not have a valid folder name.", dir);
				continue;
			}

			var strSeriesId = folderName.Split('-').FirstOrDefault();
			if (!int.TryParse(strSeriesId, out var seriesId))
			{
				_logger.LogInformation("Skipping directory {Directory} as it does not have a valid series ID.", dir);
				continue;
			}

			_logger.LogInformation("Processing series ID {SeriesId} from directory {Directory}.", seriesId, dir);
			if (!await ProcessSeries(seriesId, dir, token))
			{
				_logger.LogError("Failed to process series ID {SeriesId} from directory {Directory}.", seriesId, dir);
				return false;
			}

			_logger.LogInformation("Successfully processed series ID {SeriesId} from directory {Directory}.", seriesId, dir);
		}

		return true;
	}

	public partial record class ChapterFile(
		string Title,
		string Url,
		string Content);

	[GeneratedRegex("[^a-zA-Z0-9 ]")]
	private static partial Regex NonAlphaNumeric();

	[GeneratedRegex("(?i)<h1>(.*?)</h1>", RegexOptions.None, "en-US")]
	private static partial Regex H1Regex();
}
