namespace CardboardBox.LightNovel.Core.Sources
{
	using Anime;
	using System.Threading.Tasks;

	public interface IShSourceService : ISourceService { }
	public class ShSourceService : SourceService, IShSourceService
	{
		public override string Name => "sh";

		public override string RootUrl => "https://www.scribblehub.com";

		public ShSourceService(IApiService api, ILogger<ShSourceService> logger) : base(api, logger) { }

		public override string? GetChapter(HtmlDocument doc)
		{
			return doc.InnerText("//div[@class='chapter-title']");
		}

		public override string? GetContent(HtmlDocument doc)
		{
			var attempts = new[]
			{
				(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//div[@class='chapter-inner chapter-content']"),
				(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//div[@class='chp_raw']")
			};

			foreach(var attempt in attempts)
			{
				var content = attempt(doc);
				if (content == null || string.IsNullOrEmpty(content.InnerHtml)) continue;

				foreach(var child in content.ChildNodes.ToArray())
				{
					if (child.Name == "div")
						content.RemoveChild(child);

					if (child.Name == "p" && child.InnerText.HTMLDecode().IsWhiteSpace())
						content.RemoveChild(child);
				}

				return content.InnerHtml.HTMLDecode();
			}

			return null;
		}

		public override string? GetNextLink(HtmlDocument doc)
		{
			return doc.Attribute("//div[@class='prenext']/a[@class='btn-wi btn-next']", "href");
		}

		public override string? GetTitle(HtmlDocument doc)
		{
			return doc.InnerText("//div[@class='chp_byauthor']/a");
		}

		public override string? SeriesTitle(HtmlDocument doc)
		{
			return doc.InnerText("//div[@class='fic_title']");
		}

		public override string? SeriesAuthor(HtmlDocument doc)
		{
			return doc.InnerText("//span/a/span[@class='auth_name_fic']");
		}

		public override string? SeriesDescription(HtmlDocument doc)
		{
			return doc.InnerHtml("//div[@class='fic_row details']/div[@class='wi_fic_desc']");
		}

		public override string? SeriesImage(HtmlDocument doc)
		{
			return doc.Attribute("//div[@class='novel-cover']/div[@class='fic_image']/img", "src");
		}

		public override string[] SeriesTags(HtmlDocument doc)
		{
			return doc.DocumentNode
				.SelectNodes("//a[@id='etagme']")
				.Select(t => t.InnerText.HTMLDecode())
				.ToArray();
		}

		public override string[] SeriesGenres(HtmlDocument doc)
		{
			return doc.DocumentNode
				.SelectNodes("//span[@property='genre']/a")
				.Select(t => t.InnerText.HTMLDecode())
				.ToArray();
		}

		public override string SeriesFromChapter(string url)
		{
			var regex = new Regex("https://www.scribblehub.com/read/([0-9]{1,})-(.*?)/chapter/([0-9]{1,})(/?)");
			var parts = regex.Match(url);
			var id = parts.Groups[1].Value;
			var name = parts.Groups[2].Value;

			return $"https://www.scribblehub.com/series/{id}/{name}/";
		}

		public override string? SeriesFirstChapter(HtmlDocument doc)
		{
			return doc.Attribute("//div[@class='read_buttons']/a", "href");
		}
	}
}
