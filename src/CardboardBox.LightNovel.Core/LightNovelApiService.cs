using CardboardBox.Http;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CardboardBox.LightNovel.Core
{
	using Anime;
	using Anime.Database;

	public interface ILightNovelApiService
	{
		Task<Chapter[]?> FromJson(string path);
		Task<Chapter[]> Chapters(string url, string rootUrl);

		IEnumerable<DbChapter> FromChapters(Chapter[] chapters);
		DbChapter FromChapter(Chapter chapter, DbChapter? last);
	}

	public class LightNovelApiService : ILightNovelApiService
	{
		private readonly ILogger _logger;
		private readonly IApiService _api;

		public LightNovelApiService(
			ILogger<LightNovelApiService> logger, 
			IApiService api)
		{
			_logger = logger;
			_api = api;
		}

		public async Task<Chapter[]?> FromJson(string path)
		{
			using var io = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<Chapter[]>(io).AsTask();
		}

		public async Task<Chapter[]> Chapters(string firstUrl, string rootUrl)
		{
			var url = firstUrl;
			var chaps = new List<Chapter>();

			var count = 0;
			var max = 422;

			while (true)
			{
				count++;
				var chap = await GetChapter(url, rootUrl);
				chaps.Add(chap);

				if (string.IsNullOrEmpty(chap.NextUrl) || count > max)
					break;

				url = chap.NextUrl;
			}

			return chaps.ToArray();
		}

		public async Task<Chapter> GetChapter(string url, string rootUrl)
		{
			if (url.StartsWith("/"))
				url = $"{rootUrl.TrimEnd('/')}{url}";

			var doc = await _api.GetHtml(url);
			var title = GetTitle(doc);
			var chapter = GetChapter(doc);
			var next = GetNextLink(doc);
			var content = GetContent(doc);

			if (next.StartsWith("/"))
				next = $"{rootUrl.TrimEnd('/')}{next}";

			var validUrl = Uri.TryCreate(next, UriKind.Absolute, out var res) && (res.Scheme == Uri.UriSchemeHttp || res.Scheme == Uri.UriSchemeHttps);

			if (string.IsNullOrEmpty(next) ||
				!Uri.IsWellFormedUriString(next, UriKind.Absolute) ||
				!validUrl)
				next = "";

			return new Chapter(title, chapter, content, next, url);
		}

		public string GetTitle(HtmlDocument doc)
		{
			return doc.DocumentNode.SelectSingleNode("//h1/a[@class='booktitle']").GetAttributeValue("title", "").HTMLDecode();
		}

		public string GetChapter(HtmlDocument doc)
		{
			return doc.DocumentNode.SelectSingleNode("//h1/span[@class='chapter-title']").InnerText.HTMLDecode();
		}

		public string GetContent(HtmlDocument doc)
		{
			var chapter = doc.DocumentNode.SelectSingleNode("//div[@id='chapter-container']");
			foreach (var child in chapter.ChildNodes.ToArray())
			{
				if (child.Name == "div")
					chapter.RemoveChild(child);
			}

			return chapter.InnerHtml;
		}

		public string GetNextLink(HtmlDocument doc)
		{
			return doc.DocumentNode.SelectSingleNode("//a[@rel='next']").GetAttributeValue("href", "").HTMLDecode();
		}

		public IEnumerable<DbChapter> FromChapters(Chapter[] chapters)
		{
			DbChapter? last = null;
			foreach(var chapter in chapters)
			{
				var cur = FromChapter(chapter, last);
				last = cur;
				yield return cur;
			}
		}

		public DbChapter FromChapter(Chapter chapter, DbChapter? last)
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
	}
}
