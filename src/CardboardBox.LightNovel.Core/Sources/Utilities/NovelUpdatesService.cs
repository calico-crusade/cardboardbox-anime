namespace CardboardBox.LightNovel.Core.Sources.Utilities;

public interface INovelUpdatesService
{
	Task<TempSeriesInfo?> Series(string url);

    Task<(TempSeriesInfo? info, SourceChapterItem[] chapters)> GetChapters(string url);
}

public class NovelUpdatesService : INovelUpdatesService
{
	private readonly IApiService _api;

	public NovelUpdatesService(IApiService api)
	{
		_api = api;
	}

	public async Task<TempSeriesInfo?> Series(string url)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		return Series(doc);
	}

	public TempSeriesInfo? Series(HtmlDocument doc)
	{
        string? title = doc.InnerText("//div[@class='seriestitlenu']"),
                description = doc.InnerHtml("//div[@id='editdescription']"),
                image = doc.Attribute("//div[@class='seriesimg']/img", "src");

        if (string.IsNullOrWhiteSpace(title)) return null;

        var authors = doc
            .DocumentNode
            .SelectNodes("//div[@id='showauthors']/a")?
            .Select(t => t.InnerText.HTMLDecode())
            .ToArray() ?? Array.Empty<string>();

        var tags = doc
            .DocumentNode
            .SelectNodes("//div[@id='showtags']/a[@id='etagme']")?
            .Select(t => t.InnerText.HTMLDecode())
            .ToArray() ?? Array.Empty<string>();

        var genres = doc
            .DocumentNode
            .SelectNodes("//div[@id='seriesgenre']/a[@class='genre']")?
            .Select(t => t.InnerText.HTMLDecode())
            .ToArray() ?? Array.Empty<string>();

        return new TempSeriesInfo(title, description, authors, image, null, genres, tags);
    }

    public async Task<SourceChapterItem[]> GetChapters(string? postid, string? filter, string? grrs)
    {
        const string URL = "https://www.novelupdates.com/wp-admin/admin-ajax.php";
        var items = new List<(string, string)>
        {
            ("action", "nd_getchapters"),
        };

        if (!string.IsNullOrEmpty(postid))
            items.Add(("mypostid", postid));
        if (!string.IsNullOrEmpty(filter))
            items.Add(("mygrpfilter", filter));
        if (!string.IsNullOrEmpty(grrs))
            items.Add(("mygrr", grrs));

        var doc = await _api.PostHtml(URL, items.ToArray());
        if (doc == null) return Array.Empty<SourceChapterItem>();

        return doc.DocumentNode.SelectNodes("//ol/li/a[not(contains(title, 'Go to chapter page'))]")
            .Select(t => new SourceChapterItem
            {
                Title = t.InnerText.HTMLDecode().Trim(),
                Url = "https:" + t.GetAttributeValue("href", "")
            })
            .Where(t => !string.IsNullOrEmpty(t.Title))
            .ToArray();
    }

	public async Task<(TempSeriesInfo? info, SourceChapterItem[] chapters)> GetChapters(string url)
    { 
        var doc = await _api.GetHtml(url);
        if (doc == null) return (null, Array.Empty<SourceChapterItem>());

        var info = Series(doc);
        if (info == null) return (null, Array.Empty<SourceChapterItem>());

        string? GetValue(string id)
        {
            var value = doc?.GetElementbyId(id)?.GetAttributeValue("value", "");
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value;
        }

        string? postid = GetValue("mypostid"),
            filter = GetValue("mygrpfilter"),
            ggr = GetValue("grr_groups") ?? "0";

        var chaps = await GetChapters(postid, filter, ggr);
        return (info, chaps);
    }
}
