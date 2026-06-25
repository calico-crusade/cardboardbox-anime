using CommandLine;
using CardboardBox.Extensions;
using System.Runtime.CompilerServices;

namespace CardboardBox.Anime.Cli.Verbs;

using LightNovel.Core;
using LightNovel.Core.Sources.Utilities;

[Verb("patreon", HelpText = "Load novels from patreon sources (RR specifically)")]
public class ManualPatreonLoadOptions
{
	[Option('d', "directory", Required = true, HelpText = "The directory to load from")]
	public string? Directory { get; set; }

	[Option('s', "series-id", Required = true, HelpText = "The series id to load into")]
	public int? SeriesId { get; set; }

	[Option('u', "base-url", Required = true, HelpText = "The base URL to use for mapping pages")]
	public string? BaseUrl { get; set; }
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
		var files = Directory.GetFiles(directory, "*.html")
			.OrderBy(t => int.TryParse(Path.GetFileNameWithoutExtension(t).TrimEnd('.'), out var val) ? val : int.MaxValue);
		foreach(var file in files)
		{
			var text = await File.ReadAllTextAsync(file, token);
			var title = H1Regex().Match(text).Groups[1].Value;
			var url = GenerateUrl(baseUrl, title);
			var result = await _smart.GetCleanArticle(text, url);
			yield return new(title, url, result.content?.ForceNull() ?? text);
		}
	}

	public override async Task<bool> Execute(ManualPatreonLoadOptions options, CancellationToken token)
	{
		if (options is null || options.SeriesId is null 
			|| string.IsNullOrEmpty(options.Directory) 
			|| string.IsNullOrEmpty(options.BaseUrl))
		{
			_logger.LogError("Invalid options provided. SeriesId: {SeriesId}, Directory: {Directory}, BaseUrl: {BaseUrl}", 
				options?.SeriesId, options?.Directory, options?.BaseUrl);
			return false;
		}

		var seriesId = options.SeriesId.Value;
		var directory = options.Directory;
		var series = await _db.Series.Scaffold(seriesId);
		if (series is null)
		{
			_logger.LogError("Series with ID {SeriesId} not found in the database.", seriesId);
			return false;
		}

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

		var chapters = LoadChapters(directory, options.BaseUrl, token);
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
		}

		_logger.LogInformation("Finished loading chapters. Total new chapters loaded: {Loaded}", loaded);

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
