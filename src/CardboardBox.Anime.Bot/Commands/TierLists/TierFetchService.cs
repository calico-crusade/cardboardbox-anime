using CardboardBox.Json;
using CardboardBox.Extensions;

namespace CardboardBox.Anime.Bot.Commands.TierLists;

public interface ITierFetchService
{
    Task<TierList?> FetchTierList(string url);
}

public class TierFetchService(IApiService _api, IJsonService _json) : ITierFetchService
{
    private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/105.0.0.0 Safari/537.36";
    private const string IMAGE_CACHE_DIR = "tier-image-cache";
    private const int IMAGE_SIZE = 100;

    public async Task<TierList?> FetchTierList(string url)
    {
        var (doc, cookie) = await GetHtml(url);
        if (doc == null) return null;

        var title = doc.DocumentNode.Attribute("//meta[@property='og:description']", "content");

        var tiers = doc.DocumentNode.SelectNodes(
            "//div[@id='tier-container']" +
            "/div[@class='tier-row']" +
            "/div[@class='label-holder']/span")
            .Select(x => x.InnerText)
            .ToArray();

        if (string.IsNullOrEmpty(title)) return null;

        var id = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        var images = await Images(id, cookie, url);

        return new TierList
        {
            Title = title,
            Url = url,
            Tiers = tiers,
            Images = images,
            Hash = url.MD5Hash()
        };
    }

    public async Task<(HtmlDocument doc, string cookie)> GetHtml(string url)
    {
        var req = await ((IHttpBuilder)_api.Create(url, _json, "GET")
            .Accept("text/html")
            .Message(c =>
            {
                c.Headers.Add("user-agent", USER_AGENT);
            }))
            .Result() ?? throw new NullReferenceException($"Request returned null for: {url}");

        req.EnsureSuccessStatusCode();

        var cookies = string.Join(";", req.Headers
            .GetValues("set-cookie")
            .SelectMany(x => x.Split(';'))
            .Distinct()
            .ToArray());

        using var io = await req.Content.ReadAsStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(io);

        return (doc, cookies);
    }

    public async Task<string[]> Images(string id, string cookie, string referer)
    {
        var url = $"https://tiermaker.com/api/?type=templates-v2&id={id}";
        var urls = await _api.Get<string[]>(url, c => c.Message(c => 
        {
            c.Headers.Add("user-agent", USER_AGENT);
            c.Headers.Add("cookie", cookie);
            c.Headers.Add("referer", referer);
        })) ?? Array.Empty<string>();
        if (urls.Length == 0) return Array.Empty<string>();

        var sourceSet = "https://tiermaker.com/images" + urls.First() + "/";
        return urls.Skip(1).Select(x => sourceSet + x).ToArray();
    }

    public async Task<Stream> FetchImage(string url)
    {
        static string GetFileName(string url)
        {
            var filename = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
            var ext = Path.GetExtension(filename).Trim('.');
            var hash = url.MD5Hash();
            return Path.Combine(IMAGE_CACHE_DIR, $"{hash}.{ext}");
        }

        if (!Directory.Exists(IMAGE_CACHE_DIR)) Directory.CreateDirectory(IMAGE_CACHE_DIR);

        var filename = GetFileName(url);
        if (File.Exists(filename)) return File.OpenRead(filename);

        var ms = new MemoryStream();
        var (data, _, _, _) = await _api.GetData(url);
        await data.CopyToAsync(ms);
        await data.DisposeAsync();
        ms.Position = 0;

        using var output = File.OpenWrite(filename);
        await ms.CopyToAsync(output);
        await output.FlushAsync();

        ms.Position = 0;
        return ms;
    }
}
