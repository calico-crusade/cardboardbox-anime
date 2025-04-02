namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;

public interface IReLibSourceService : ISourceVolumeService { }

public class ReLibSourceService : IReLibSourceService
{
	private readonly IApiService _api;
	private readonly ILogger<ReLibSourceService> _logger;
	private readonly INovelUpdatesService _info;

	public string Name => "relib";

	public string RootUrl => "https://re-library.com";

	public ReLibSourceService(
		IApiService api, 
		ILogger<ReLibSourceService> logger,
		INovelUpdatesService info) 
	{
		_api = api;
		_logger = logger;
		_info = info;
	}

	public async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
	{
		string url = firstUrl,
			   seriesUrl = SeriesFromChapter(firstUrl);
		var series = await GetSeriesInfo(seriesUrl);
		if (series == null) yield break;

		int count = 0;
		while(true)
		{
			count++;
			var chap = await GetChapter(url, series.Title);
			if (chap == null) yield break;

			yield return chap;

			if (string.IsNullOrEmpty(chap.NextUrl) || chap.NextUrl.EndsWith("/coming-soon/"))
				break;

			url = chap.NextUrl;
		}
	}

	public async IAsyncEnumerable<SourceVolume> Volumes(string seriesUrl)
	{
		var doc = await _api.GetHtml(seriesUrl);
		if (doc == null) yield break;

		var volumes = doc.DocumentNode.SelectNodes("//div[@class='su-accordion su-u-trim']/div[starts-with(@class, 'su-spoiler su-spoiler-style-default')]");

		foreach(var vol in volumes)
		{
			var d = new HtmlDocument();
			d.LoadHtml(vol.InnerHtml);
			var title = d.InnerText("//div[@class='su-spoiler-title']");
			var chaps = d.DocumentNode.SelectNodes("//div[@class='su-spoiler-content su-u-clearfix su-u-trim']/ul/li[starts-with(@class, 'page_item page-item-')]/a");
			string? url = null;

			if (string.IsNullOrWhiteSpace(title))
				throw new ArgumentException("Title cannot be null for volume!");

			var chapters = new List<SourceChapterItem>();

			foreach(var chap in chaps)
			{
				var chapTitle = chap.InnerText.HTMLDecode();
				var chapUrl = chap.GetAttributeValue("href", "");

				if (string.IsNullOrEmpty(url))
					url = VolumeFromChapter(chapUrl);

				chapters.Add(new SourceChapterItem
				{
					Title = chapTitle,
					Url = chapUrl ?? ""
				});
			}

			yield return new SourceVolume
			{
				Title = title,
				Url = url ?? string.Empty,
				Chapters = [..chapters]
			};
		}
	}

	public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var title = doc.InnerText("//h1[@class='entry-title']");
		if (string.IsNullOrWhiteSpace(title)) return null;

		var next = doc.Attribute("//div[@class='nextPageLink']/a", "href");

		var contentParent = doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']");
		bool incontent = false, outcontent = false;
		var children = contentParent.ChildNodes.ToArray();
		foreach(var child in children)
		{
			if (!incontent)
			{
				if (child.Name == "p" && child.InnerHtml.Contains("<span id=\"more-"))
					incontent = true;

				contentParent.RemoveChild(child);
				continue;
			}

			if (child.Name == "hr" && child.GetAttributeValue("id", "") == "ref") outcontent = true;

			if (outcontent)
			{
				contentParent.RemoveChild(child);
				continue;
			}

			if (child.Name == "div") contentParent.RemoveChild(child);

			if (child.Name == "p" && (
					child.InnerHtml.Contains("<span id='easy-footnote-") ||
					child.InnerHtml.Contains("<span class='easy-footnote'>")
				))
			{
				foreach (var ic in child.ChildNodes.ToArray())
					if (ic.Name == "span" && (
							ic.GetAttributeValue("id", "").StartsWith("easy-footnote-") ||
							ic.GetAttributeValue("class", "") == "easy-footnote"
						))
						child.RemoveChild(ic);
			}
		}

		var content = contentParent.InnerHtml;
		return new SourceChapter
		{
			ChapterTitle = title,
			Content = content,
			Url = url,
			NextUrl = next ?? "",
			BookTitle = bookTitle
		};
	}

	public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		var nul = doc.Attribute("//a[starts-with(@href, 'https://www.novelupdates.com/series/')]", "href");
		if (string.IsNullOrWhiteSpace(nul)) return null;

		var series = await _info.Series(nul);
		if (series == null) return null;

		var fc = doc.Attribute("//div[@class='su-spoiler-content su-u-clearfix su-u-trim']/ul/li[starts-with(@class, 'page_item page-item-')]/a", "href");

		return new TempSeriesInfo(series.Title, series.Description, series.Authors, series.Image, fc, series.Genre, series.Tags);
	}

	public string SeriesFromChapter(string url)
	{
		var regex = new Regex("https://re-library.com/translations/(.*?)/(.*?)/(.*?)");
		var parts = regex.Match(url);
		var series = parts.Groups[1].Value;

		return $"https://re-library.com/translations/{series}/";
	}

	public string VolumeFromChapter(string url)
	{
		var regex = new Regex("https://re-library.com/translations/(.*?)/(.*?)/(.*?)");
		var parts = regex.Match(url);
		var series = parts.Groups[1].Value;
		var volume = parts.Groups[2].Value;

		return $"https://re-library.com/translations/{series}/{volume}/";
	}
}
