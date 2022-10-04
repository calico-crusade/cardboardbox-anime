namespace CardboardBox.LightNovel.Core.Sources
{
	public interface ISource1Service : ISourceService { }

	public class Source1Service : SourceService, ISource1Service
	{
		public Source1Service(IApiService api, ILogger<Source1Service> logger) : base(api, logger) { }

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
