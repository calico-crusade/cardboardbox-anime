namespace CardboardBox.LightNovel.Core;

public interface ISourceService
{
	string Name { get; }
	string RootUrl { get; }

	IAsyncEnumerable<SourceChapter> Chapters(string firstUrl);

	Task<TempSeriesInfo?> GetSeriesInfo(string url);

	string SeriesFromChapter(string url);
}

public interface ISourceVolumeService : ISourceService
{
	IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl);

	Task<SourceChapter?> GetChapter(string url, string bookTitle);
}

public abstract class SourceService : ISourceService
{
	public readonly IApiService _api;
	public readonly ILogger _logger;

	public abstract string Name { get; }
	public abstract string RootUrl { get; }

	public SourceService(IApiService api, ILogger logger)
	{
		_api = api;
		_logger = logger;
	}

	public virtual async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
	{
		string rootUrl = firstUrl.GetRootUrl(),
			   url = firstUrl;

		int count = 0;
		
		while(true)
		{
			count++;
			var chap = await GetChapter(url, rootUrl);

			yield return chap;

			if (string.IsNullOrEmpty(chap.NextUrl))
				break;

			url = chap.NextUrl;
		}
	}

	public virtual async Task<SourceChapter> GetChapter(string url, string rootUrl)
	{
		if (url.StartsWith("/"))
			url = $"{rootUrl.TrimEnd('/')}{url}";

		var doc = await _api.GetHtml(url);
		var title = GetTitle(doc);
		var chapter = GetChapter(doc);
		var next = GetNextLink(doc);
		var content = GetContent(doc);

		if (!string.IsNullOrEmpty(next) && next.StartsWith("/"))
			next = $"{rootUrl.TrimEnd('/')}{next}";

		var validUrl = Uri.TryCreate(next, UriKind.Absolute, out var res) && 
			(res.Scheme == Uri.UriSchemeHttp || res.Scheme == Uri.UriSchemeHttps);

		if (string.IsNullOrEmpty(next) ||
			!Uri.IsWellFormedUriString(next, UriKind.Absolute) ||
			!validUrl)
			next = "";

		return new SourceChapter(
			title ?? string.Empty, 
			chapter ?? string.Empty, 
			content ?? string.Empty, 
			next, 
			url);
	}

	public abstract string? GetTitle(HtmlDocument doc);
	public abstract string? GetChapter(HtmlDocument doc);
	public abstract string? GetContent(HtmlDocument doc);
	public abstract string? GetNextLink(HtmlDocument doc);

	public virtual async Task<TempSeriesInfo?> GetSeriesInfo(string url)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		string? title = SeriesTitle(doc),
				author = SeriesAuthor(doc),
				descrip = SeriesDescription(doc),
				image = SeriesImage(doc),
				firstChap = SeriesFirstChapter(doc);
		string[] tags = SeriesTags(doc),
				 genres = SeriesGenres(doc);

		var authors = string.IsNullOrEmpty(author) ? Array.Empty<string>() : new[] { author };

		if (string.IsNullOrEmpty(title)) return null;

		return new TempSeriesInfo(title, descrip, authors, image, firstChap, genres, tags);
	}

	public abstract string? SeriesTitle(HtmlDocument doc);
	public abstract string? SeriesAuthor(HtmlDocument doc);
	public abstract string? SeriesDescription(HtmlDocument doc);
	public abstract string? SeriesImage(HtmlDocument doc);
	public abstract string? SeriesFirstChapter(HtmlDocument doc);
	public abstract string[] SeriesTags(HtmlDocument doc);
	public abstract string[] SeriesGenres(HtmlDocument doc);

	public abstract string SeriesFromChapter(string url);
}

public record class TempSeriesInfo(
	string Title, 
	string? Description, 
	string[] Authors, 
	string? Image, 
	string? FirstChapterUrl, 
	string[] Genre, 
	string[] Tags);
