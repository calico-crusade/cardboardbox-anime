namespace CardboardBox.LightNovel.Core.Sources
{
	public interface ILnpSourceService : ISourceService { }

	public class LnpSourceService : SourceService, ILnpSourceService
	{
		public override string Name => "lnp";
		public override string RootUrl => "https://www.lightnovelpub.com";

		public LnpSourceService(IApiService api, ILogger<LnpSourceService> logger) : base(api, logger) { }

		public override string? GetTitle(HtmlDocument doc)
		{
			return doc.Attribute("//h1/a[@class='booktitle']", "title");
		}

		public override string? GetChapter(HtmlDocument doc)
		{
			return doc.InnerText("//h1/span[@class='chapter-title']");
		}

		public override string? GetContent(HtmlDocument doc)
		{
			var chapter = doc.DocumentNode.SelectSingleNode("//div[@id='chapter-container']");
			if (chapter == null || chapter.ChildNodes == null) return null;

			foreach (var child in chapter.ChildNodes.ToArray())
			{
				if (child.Name == "div")
					chapter.RemoveChild(child);
			}

			return chapter.InnerHtml;
		}

		public override string? GetNextLink(HtmlDocument doc)
		{
			return doc.Attribute("//a[@rel='next']", "href");
		}
	}
}
