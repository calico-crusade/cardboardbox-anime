namespace CardboardBox.LightNovel.Core.Sources;

using Utilities.FlareSolver;

public interface IShSourceService : ISourceService { }

public class ShSourceService(
	IApiService api, 
	IFlareSolver _flare, 
	ILogger<ShSourceService> logger) : SourceService(api, logger), IShSourceService
{
	private SolverCookie[]? _cookies = null;

	public override string Name => "sh";

	public override string RootUrl => "https://www.scribblehub.com";

	public override int MaxRequestsBeforePauseMin => 0;
	public override int MaxRequestsBeforePauseMax => 0;
	public override int PauseDurationSecondsMin => 0;
	public override int PauseDurationSecondsMax => 0;

	public async Task<HtmlDocument> DoRequest(string url, bool first = true)
	{
        try
        {
            var data = await _flare.Get(url, _cookies, timeout: 30_000);
            if (data is null || data.Solution is null) throw new Exception("Failed to get data");

            if (data.Solution.Status < 200 || data.Solution.Status >= 300)
                throw new Exception($"Failed to get data: {data.Solution.Status}");

            _cookies = data.Solution.Cookies;

            var doc = new HtmlDocument();
            doc.LoadHtml(data.Solution.Response);
            return doc;
        }
        catch (Exception ex)
        {
			if (!first) throw;

			_cookies = null;
			var delay = Random.Shared.Next(30, 80);
            _logger.LogError(ex, "Failed to get data, retrying after {delay} seconds", delay);
            await Task.Delay(delay * 1000);
			_logger.LogInformation("Retrying request");
            return await DoRequest(url, false);
        }
    }


    public override Task<HtmlDocument> Get(string url)
    {
		return DoRequest(url);
    }

    public override string? GetChapter(HtmlDocument doc)
	{
		return doc.InnerText("//div[@class='chapter-title']");
	}

	public override string? GetContent(HtmlDocument doc)
	{
		var attempts = new[]
		{
			(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//div[@class='chapter-inner chapter-content']"),
			(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//div[@class='chp_raw']"),
			(HtmlDocument doc) => doc.DocumentNode.SelectSingleNode("//div[@class='chp_raw']/div")
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

			var outputContent = content.InnerHtml.Trim('\r').Trim('\n');
			if (string.IsNullOrEmpty(outputContent)) continue;

			return outputContent;
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
			.SelectNodes("//a[@id='etagme']")?
			.Select(t => t?.InnerText?.HTMLDecode()!)
			.Where(t => !string.IsNullOrEmpty(t))
			.ToArray() ?? Array.Empty<string>();
	}

	public override string[] SeriesGenres(HtmlDocument doc)
	{
		return doc.DocumentNode
			.SelectNodes("//span[@property='genre']/a")?
			.Select(t => t?.InnerText?.HTMLDecode()!)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToArray() ?? Array.Empty<string>();
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
