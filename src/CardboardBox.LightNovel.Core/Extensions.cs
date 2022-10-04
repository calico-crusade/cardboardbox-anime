using System.Web;

namespace CardboardBox.LightNovel.Core
{
	using Sources;

	public static class Extensions
	{
		public static async Task<HtmlDocument> GetHtml(this IApiService api, string url, Action<HttpRequestMessage>? config = null)
		{
			const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";

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

		public static string HTMLDecode(this string text)
		{
			return HttpUtility.HtmlDecode(text);
		}

		public static string PurgePathChars(this string text)
		{
			var chars = Path.GetInvalidFileNameChars()
				.Union(Path.GetInvalidPathChars())
				.ToArray();

			foreach(var c in chars)
				text = text.Replace(c.ToString(), "");

			return text;
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

		public static IServiceCollection AddLightNovel(this IServiceCollection services)
		{
			return services
				.AddTransient<ISource1Service, Source1Service>()
				.AddTransient<ISource2Service, Source2Service>()

				.AddTransient<ILightNovelApiService, LightNovelApiService>()
				.AddTransient<IPdfService, PdfService>();
		}
	}
}
