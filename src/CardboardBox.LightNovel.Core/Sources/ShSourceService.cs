namespace CardboardBox.LightNovel.Core.Sources
{
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
	}
}
