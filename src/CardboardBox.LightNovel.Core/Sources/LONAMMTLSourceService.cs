namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface ILONAMMTLSourceService : ISourceVolumeService { }

internal class LONAMMTLSourceService(
	IFlareSolver _flare,
	ISmartReaderService _smart,
	ILogger<LONAMMTLSourceService> _logger) : FlareVolumeSource(_flare, _smart, _logger), ILONAMMTLSourceService
{
	public override string Name => "lonammtl";

	public override string RootUrl => "https://lilysobservatory.blogspot.com";

    internal const string POST_XPATH = "//div[@class='post-body entry-content float-container']";

    public override int MaxRequestsBeforePauseMax => 20;

    public override int MaxRequestsBeforePauseMin => 10;

    public static HtmlDocument? GetBlogSpot(HtmlDocument doc, bool includeHead = true)
    {
		var portion = doc.DocumentNode.SelectSingleNode(POST_XPATH);
		if (portion is null) return null;

        doc.LoadHtml(portion.OuterHtml);
        return doc;
	}

    public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
		var doc = await base.Get(url, true);
        if (doc is null) return null;

        var title = doc.InnerText("//head/title");
        if (string.IsNullOrEmpty(title)) return null;

        doc = GetBlogSpot(doc);
        if (doc is null) return null;

        var content = doc.DocumentNode.SelectSingleNode(POST_XPATH);
        if (content is null) return null;

        var iterator = new HtmlTraverser(content);
        var imgEl = iterator.MoveUntil(t => t.SelectSingleNode(".//img") is not null);
        if (imgEl is null) return null;

        var imageUrl = imgEl.SelectSingleNode(".//img").GetAttributeValue("src", "");

        var authorEl = iterator.MoveUntil(t => t.InnerText.Contains("author:", StringComparison.OrdinalIgnoreCase));
        if (authorEl is null) return null;

        var author = authorEl.InnerText.HTMLDecode().Replace("author:", "", StringComparison.OrdinalIgnoreCase).Trim();

        var descriptionEls = iterator.EverythingUntil(t => t.SelectSingleNode(".//a") is not null);
        var description = descriptionEls.Join(true);

        var chapter = (await ParseVolumes(doc, url).FirstOrDefaultAsync())?.Chapters.FirstOrDefault()?.Url;

        return new(title, description, [author], imageUrl, chapter, [], []);
	}

    public override string? NextUrl(HtmlDocument doc, string url)
    {
        return GetNavButtonText(doc, "next");
    }

    public string? GetNavButtonText(HtmlDocument doc, string text)
    {
		var url = doc.DocumentNode.SelectNodes("//div[@class='nav-buttons']/a")?
			.FirstOrDefault(t => t.InnerText?.Contains(text, StringComparison.InvariantCultureIgnoreCase) ?? false)?
            .GetAttributeValue("href", "");
        if (!string.IsNullOrEmpty(url)) return url;

        return doc.DocumentNode.SelectNodes("//a")
            .FirstOrDefault(t =>
                t.InnerText?.Contains(text, StringComparison.InvariantCultureIgnoreCase) ?? false &&
                t.GetAttributeValue("href", "").StartsWith(RootUrl, StringComparison.InvariantCultureIgnoreCase))?
            .GetAttributeValue("href", "");
	}

    public override (string? title, string? content) BackupParse(HtmlDocument doc, string url)
    {
        var mod = GetBlogSpot(doc);
        if (mod is null) return (null, null);

        var title = doc.DocumentNode.InnerText("//head/title");

        var html = mod.DocumentNode.SelectNodes("//html");
        html.Each(t => t.Remove());

        var output = _smart.CleanseHtml(mod.DocumentNode.InnerHtml, url);
        return (title, output);
    }

    public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument orginal, string url)
    {
        var doc = GetBlogSpot(orginal);
        if (doc is null)
        {
            _logger.LogWarning("Could not find blog content");
            yield break;
        }
        var content = doc.DocumentNode.SelectSingleNode(POST_XPATH);
        if (content is null)
		{
			_logger.LogWarning("Could not find blog content - inner");
			yield break;
		}

		var iterator = new HtmlTraverser(content);
        var check = iterator.MoveUntil(t => t.InnerText.Contains("Table of Contents", StringComparison.OrdinalIgnoreCase));
        if (check is null)
		{
			_logger.LogWarning("Could not find ToC signifier");
			yield break;
		}

        var fullToc = iterator
            .EverythingUntil(t => t.InnerText?.HTMLDecode().Contains("——————") ?? false)
            .Where(t => !string.IsNullOrEmpty(t.InnerText.Trim()))
#if DEBUG
            .ToArray()
#endif
            ;

        if (!fullToc.Any())
        {
            _logger.LogWarning("Could not find ToC");
            yield break;
        }

        string? title = null;
        var chapters = new List<SourceChapterItem>();

        foreach(var element in fullToc)
		{
			var inner = element.InnerText.Replace("&#8226;", string.Empty).HTMLDecode().Trim();
			if (inner.Contains("chapter", StringComparison.InvariantCultureIgnoreCase))
            {
                if (!string.IsNullOrEmpty(title) && chapters.Count > 0)
                {
                    yield return new SourceVolume
                    {
                        Title = title,
                        Chapters = [.. chapters]
                    };
					chapters.Clear();
				}

				title = inner;
				continue;
			}

			var chapter = element.SelectSingleNode(".//a");
			if (chapter is null) continue;

			chapters.Add(new()
            {
                Title = chapter.InnerText?.HTMLDecode().Trim() ?? string.Empty,
                Url = chapter.GetAttributeValue("href", "").Trim()
			});
        }

        if (string.IsNullOrEmpty(title) || chapters.Count == 0) yield break;

		yield return new SourceVolume
		{
			Title = title,
			Chapters = [.. chapters]
		};
	}

    public override string SeriesFromChapter(string url)
    {
        async Task<string?> FetchSeriesFile(string url)
        {
            var doc = await Get(url, true);
            if (doc is null) return null;

			return GetNavButtonText(doc, "Table of Contents");
		}


        var task = FetchSeriesFile(url);
        task.Wait();
        return task.Result ?? string.Empty;
	}
}
