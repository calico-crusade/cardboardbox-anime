namespace CardboardBox.LightNovel.Core.Sources;

using Anime;

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

	public override string? SeriesTitle(HtmlDocument doc)
	{
		return doc.Attribute("//figure/img[@class='lazyload']", "alt");
	}

	public override string? SeriesAuthor(HtmlDocument doc)
	{
		return doc.InnerText("//div[@class='author']/a/span[@itemprop='author']");
	}

	public override string? SeriesDescription(HtmlDocument doc)
	{
		var content = doc.DocumentNode.SelectSingleNode("//div[@class='summary']/div[@class='content expand-wrapper']");
		if (content == null || content.ChildNodes == null) return null;

		foreach (var child in content.ChildNodes.ToArray())
		{
			if (child.Name == "div")
				content.RemoveChild(child);
		}

		return content.InnerHtml;
	}

	public override string? SeriesImage(HtmlDocument doc)
	{
		return doc.Attribute("//figure/img[@class='lazyload']", "data-src");
	}

	public override string[] SeriesTags(HtmlDocument doc)
	{
		return doc.DocumentNode
			.SelectNodes("//div[@class='tags']/div/ul/li/a[@class='tag']")
			.Select(t => t.InnerText.HTMLDecode())
			.ToArray();
	}

	public override string[] SeriesGenres(HtmlDocument doc)
	{
		return doc.DocumentNode
			.SelectNodes("//div[@class='categories']/ul/li/a[@class='property-item']")
			.Select(t => t.InnerText.HTMLDecode())
			.ToArray();
	}

	public override string SeriesFromChapter(string url)
	{
		return string.Join("/", url.Split('/').SkipLast());
	}

	public override string? SeriesFirstChapter(HtmlDocument doc)
	{
		return RootUrl + "/" + doc.Attribute("//a[@id='readchapterbtn']", "href");
	}
}
