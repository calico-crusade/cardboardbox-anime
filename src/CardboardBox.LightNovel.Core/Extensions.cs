using System.Web;

namespace CardboardBox.LightNovel.Core
{
	using Anime.Database.Mapping;
	using Database;
	using Sources;

	public static class Extensions
	{
		public const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";

		public static async Task<HtmlDocument> GetHtml(this IApiService api, string url, Action<HttpRequestMessage>? config = null)
		{
			var req = await api.Create(url)
				.Accept("text/html")
				.With(c =>
				{
					c.Headers.Add("user-agent", USER_AGENT);
					config?.Invoke(c);
				})
				.Result();

			if (req == null)
				throw new NullReferenceException($"Request returned null for: {url}");

			req.EnsureSuccessStatusCode();

			using var io = await req.Content.ReadAsStreamAsync();
			var doc = new HtmlDocument();
			doc.Load(io);

			return doc;
		}

		public static async Task<(Stream data, long length, string filename, string type)> GetData(this IApiService api, string url, Action<HttpRequestMessage>? config = null)
		{
			var req = await api.Create(url)
				.Accept("*/*")
				.With(c =>
				{
					c.Headers.Add("user-agent", USER_AGENT);
					config?.Invoke(c);
				})
				.Result();
			if (req == null)
				throw new NullReferenceException($"Request returned null for: {url}");

			req.EnsureSuccessStatusCode();

			var headers = req.Content.Headers;
			var path = headers?.ContentDisposition?.FileName ?? headers?.ContentDisposition?.Parameters?.FirstOrDefault()?.Value ?? "";
			var type = headers?.ContentType?.ToString() ?? "";
			var length = headers?.ContentLength ?? 0;

			return (await req.Content.ReadAsStreamAsync(), length, path, type);
		}

		public static string HTMLDecode(this string text)
		{
			return HttpUtility.HtmlDecode(text).Trim('\n');
		}

		public static string SafeSubString(this string text, int length, int start = 0)
		{
			if (start + length > text.Length)
				return text[start..];

			return text.Substring(start, length);
		}

		public static string GetRootUrl(this string url)
		{
			if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
				throw new UriFormatException(url);

			return uri.GetRootUrl();
		}

		public static string GetRootUrl(this Uri uri)
		{
			var port = uri.IsDefaultPort ? "" : ":" + uri.Port; 
			return $"{uri.Scheme}://{uri.Host}{port}";
		}

		public static string? InnerText(this HtmlDocument doc, string xpath)
		{
			return doc.DocumentNode.SelectSingleNode(xpath)?.InnerText?.HTMLDecode();
		}

		public static string? InnerHtml(this HtmlDocument doc, string xpath)
		{
			return doc.DocumentNode.SelectSingleNode(xpath)?.InnerHtml?.HTMLDecode();
		}

		public static string? Attribute(this HtmlDocument doc, string xpath, string attr)
		{
			return doc.DocumentNode.SelectSingleNode(xpath)?.GetAttributeValue(attr, "")?.HTMLDecode();
		}

		public static bool IsWhiteSpace(this string? value)
		{
			var isWs = (char c) => char.IsWhiteSpace(c) || c == '\u00A0';
			if (value == null || value.Length == 0) return true;

			for (var i = 0; i < value.Length; i++)
				if (!isWs(value[i])) return false;

			return true;
		}

		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> data, int count = 1)
		{
			return data.Reverse().Skip(count).Reverse();
		}

		public static Dictionary<T, V[]> ToGDictionary<T, V>(this IEnumerable<V> data, Func<V, T> keySelector) where T : notnull
		{
			return data
				.GroupBy(t => keySelector(t))
				.ToDictionary(t => t.Key, t => t.ToArray());
		}

		public static Dictionary<T, V[]> ToGDictionary<T, V, O>(this IEnumerable<V> data, Func<V, T> keySelector, Func<V, O> order, bool asc = true) where T : notnull
		{
			return data
				.GroupBy(t => keySelector(t))
				.ToDictionary(t => t.Key, t =>
				{
					if (asc) return t.OrderBy(order).ToArray();
					return t.OrderByDescending(order).ToArray();
				});
		}

		public static IServiceCollection AddLightNovel(this IServiceCollection services)
		{
			MapConfig.AddMap(c =>
			{
				c.ForEntity<Book>()
				 .ForEntity<Chapter>()
				 .ForEntity<Page>()
				 .ForEntity<Series>()
				 .ForEntity<ChapterPage>();
			});

			return services
				.AddTransient<ILnpSourceService, LnpSourceService>()
				.AddTransient<IShSourceService, ShSourceService>()

				.AddTransient<ILightNovelApiService, LightNovelApiService>()
				.AddTransient<IPdfService, PdfService>()
				
				.AddTransient<IDbBookService, DbBookService>()
				.AddTransient<IDbChapterService, DbChapterService>()
				.AddTransient<IDbPageService, DbPageService>()
				.AddTransient<IDbSeriesService, DbSeriesService>()
				.AddTransient<IDbChapterPageService, DbChapterPageService>()
				
				.AddTransient<ILnDbService, LnDbService>();
		}
	}
}
