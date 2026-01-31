using CardboardBox.Json;
using CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;
using System.Net;

namespace CardboardBox.LightNovel.Core.Sources.Utilities;

public interface INovelUpdatesService
{
	Task<TempSeriesInfo?> Series(string url);

    Task<(TempSeriesInfo? info, SourceChapterItem[] chapters)> GetChapters(string url);

    Task<string?> GetChapterUrl(SourceChapterItem item);
}

public class NovelUpdatesService(
    IApiService _api, 
    IFlareSolver _flare,
    IJsonService _json,
    ILogger<NovelUpdatesService> _logger) : INovelUpdatesService
{
    private SolverCookie[]? _cookies = null;

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

    public async Task<TempSeriesInfo?> Series(string url)
	{
		var doc = await DoRequest(url);
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

        return doc.DocumentNode.SelectNodes("//ol/li/a[not(contains(title, 'Go to chapter page'))]")?
            .Select(t => new SourceChapterItem
            {
                Title = t.InnerText.HTMLDecode().Trim(),
                Url = "https:" + t.GetAttributeValue("href", "")
            })
            .Where(t => !string.IsNullOrEmpty(t.Title))
            .ToArray() ?? [];
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

    public async Task<string?> GetChapterUrl(SourceChapterItem item)
    {
        var req = await ((IHttpBuilder)_api.Create(item.Url, _json, "GET")
            .Accept("text/html")
            .With(c => c.Message(c => 
            {
                c.Headers.Add("user-agent", SomeExtensions.USER_AGENT);
            }))).Result() ?? throw new NullReferenceException($"Request returned null for: {item.Url}");

        req.EnsureSuccessStatusCode();
        return req.RequestMessage?.RequestUri?.OriginalString;
    }
}
