namespace CardboardBox.LightNovel.Core
{
	using Anime;
	using Anime.Database;

	public interface ISourceService
	{
		string Name { get; }
		string RootUrl { get; }

		IAsyncEnumerable<Chapter> Chapters(string firstUrl);

		IAsyncEnumerable<DbChapter> DbChapters(string firstUrl);
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

		public virtual async IAsyncEnumerable<Chapter> Chapters(string firstUrl)
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

		public virtual async IAsyncEnumerable<DbChapter> DbChapters(string firstUrl)
		{
			DbChapter? last = null;
			await foreach(var chapter in Chapters(firstUrl))
				yield return last = FromChapter(chapter, last);
		}

		public static DbChapter FromChapter(Chapter chapter, DbChapter? last)
		{
			return new DbChapter
			{
				HashId = chapter.Url.MD5Hash(),
				BookId = chapter.BookTitle.MD5Hash(),
				Book = chapter.BookTitle,
				Chapter = chapter.ChapterTitle,
				Content = chapter.Content,
				Url = chapter.Url,
				NextUrl = chapter.NextUrl,
				Ordinal = (last?.Ordinal ?? 0) + 1
			};
		}

		public virtual async Task<Chapter> GetChapter(string url, string rootUrl)
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

			var validUrl = Uri.TryCreate(next, UriKind.Absolute, out var res) && (res.Scheme == Uri.UriSchemeHttp || res.Scheme == Uri.UriSchemeHttps);

			if (string.IsNullOrEmpty(next) ||
				!Uri.IsWellFormedUriString(next, UriKind.Absolute) ||
				!validUrl)
				next = "";

			return new Chapter(title ?? string.Empty, chapter ?? string.Empty, content ?? string.Empty, next, url);
		}

		public abstract string? GetTitle(HtmlDocument doc);
		public abstract string? GetChapter(HtmlDocument doc);
		public abstract string? GetContent(HtmlDocument doc);
		public abstract string? GetNextLink(HtmlDocument doc);
	}
}
