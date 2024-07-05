namespace CardboardBox.Manga.Providers;

public interface IRawKumaSource
{
    IAsyncEnumerable<ChapterDownloadLink> GetDownloadLinks(string url);

    Task<string?> Download(ChapterDownloadLink link, string output);

    Task<string?> DownloadFromPages(ChapterDownloadLink link, string output);
}

internal class RawKumaSource : IRawKumaSource
{
    private readonly IApiService _api;
    private readonly ILogger _logger;

    public RawKumaSource(
        IApiService api, 
        ILogger<RawKumaSource> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async IAsyncEnumerable<ChapterDownloadLink> GetDownloadLinks(string url)
    {
        var html = await _api.GetHtml(url);
        if (html is null)
        {
            _logger.LogError("Failed to get html from {url}", url);
            yield break;
        }

        var chapters = html.DocumentNode.SelectNodes("//div[@class='eplister']/ul[@class='clstyle']/li");
        if (chapters is null)
        {
            _logger.LogError("Failed to get chapters from {url}", url);
            yield break;
        }

        foreach(var li in chapters)
        {
            var num = li.GetAttributeValue("data-num", "");
            var anchor = li.SelectSingleNode("./div[@class='chbox']/div[@class='dt']/a")?.GetAttributeValue("href", "");
            var pages = li.SelectSingleNode("./div[@class='chbox']/div[@class='eph-num']/a")?.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(anchor) || string.IsNullOrEmpty(num) || string.IsNullOrEmpty(pages))
            {
                _logger.LogError("Failed to get anchor from chapter {num}", num);
                continue;
            }

            yield return new ChapterDownloadLink(num, anchor, pages);
        }
    }

    public async Task<string?> Download(ChapterDownloadLink link, string output)
    {
        if (link is null)
        {
            _logger.LogError("ChapterDownloadLink is null");
            return null;
        }

        if (!Directory.Exists(output)) Directory.CreateDirectory(output);

        
        var (stream, _, file, type) = await _api.GetData(link.Url);
        if (string.IsNullOrEmpty(file))
        {
            _logger.LogError("Failed to get file name from {url}", link.Url);
            return null;
        }

        if (!file.EndsWith(".zip"))
            file = $"{link.Chapter}.zip";

        Regex rgx = new("[^a-zA-Z0-9\\.-]");
        file = rgx.Replace(file, "");

        var path = Path.Combine(output, file);
        if (File.Exists(path))
        {
            _logger.LogWarning("Deleting existing file {path}", path);
            File.Delete(path);
        }

        using var io = File.Create(path);
        await stream.CopyToAsync(io);
        await stream.DisposeAsync();
        await io.FlushAsync();
        _logger.LogInformation("Downloaded {file} from {url} >> {path}", file, link.Url, path);
        return path;
    }

    public async Task<string?> DownloadFromPages(ChapterDownloadLink link, string output)
    {
        if (link is null)
        {
            _logger.LogError("ChapterDownloadLink is null");
            return null;
        }

        if (!Directory.Exists(output)) Directory.CreateDirectory(output);
        var chapterPath = Path.Combine(output, link.Chapter);
        if (!Directory.Exists(chapterPath)) Directory.CreateDirectory(chapterPath);

        var html = await _api.GetHtml(link.PagesUrl);
        if (html is null)
        {
            _logger.LogError("Failed to get html from {url}", link.PagesUrl);
            return null;
        }

        var pages = html.DocumentNode.SelectNodes("//div[@id='readerarea']/noscript/p/img");
        if (pages is null)
        {
            _logger.LogError("Failed to get pages from {url}", link.PagesUrl);
            return null;
        }

        var pageUrls = pages.Select(t => t.GetAttributeValue("src", ""));
        foreach(var page in pageUrls)
        {
            var (stream, _, file, type) = await _api.GetData(page);
            if (string.IsNullOrEmpty(file))
                file = page.Split('/').Last();

            Regex rgx = new("[^a-zA-Z0-9\\.-]");
            file = rgx.Replace(file, "");

            var path = Path.Combine(chapterPath, file);
            using var io = File.Create(path);
            await stream.CopyToAsync(io);
            await stream.DisposeAsync();
            await io.FlushAsync();
            _logger.LogInformation("Downloaded {file} from {url} >> {path}", file, page, path);
        }

        return chapterPath;
    }

}

public record class ChapterDownloadLink(
    string Chapter, 
    string Url,
    string PagesUrl);
