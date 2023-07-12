namespace CardboardBox.LightNovel.Core.Sources.ZirusSource;

public interface IZirusApiService
{
    Task<ZirusSeries?> Series(string seriesId);

    string BuildChapUrl(string seriesId, int volume, string chapter);

    string BuildChapUrl(string seriesId, int volume, double chapter);

    string BuildChapUrl(string seriesId, ZirusSeries.Next next);

    Task<ZirusChapter?> Chapter(string seriesId, int volume, string chapter);

    Task<ZirusChapter?> Chapter(string seriesId, int volume, double chapter);

    Task<ZirusChapter?> Chapter(string seriesId, ZirusSeries.Next next);

    Task<ZirusChapter?> Chapter(string url);
}

public class ZirusApiService : IZirusApiService
{
    private readonly IApiService _api;
    private readonly IConfiguration _config;

    public string RootUrl => "https://zirusmusings.net";

    public string SeriesKey => _config["NovelSources:ZirusKey"] ?? throw new ArgumentNullException("Zirus Key not found in config");

    public string ApiUrl => $"{RootUrl}/_next/data/{SeriesKey}/";

    public ZirusApiService(
        IApiService api,
        IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    public Task<ZirusSeries?> Series(string seriesId)
    {
        //https://zirusmusings.net/_next/data/2XCJ9PL_uxx4mf-pTDI_k/series/mg.json?seriesId=mg
        var url = $"{ApiUrl}series/{seriesId}.json?seriesId={seriesId}";
        return _api.Get<ZirusSeries>(url);
    }

    public string BuildChapUrl(string seriesId, int volume, string chapter)
    {
        return $"{ApiUrl}series/{seriesId}/{volume}/{chapter}.json?seriesId={seriesId}&firstId={volume}&secondId={chapter}";
    }

    public string BuildChapUrl(string seriesId, int volume, double chapter)
    {
        return BuildChapUrl(seriesId, volume, chapter.ToString());
    }

    public string BuildChapUrl(string seriesId, ZirusSeries.Next next)
    {
        return BuildChapUrl(seriesId, next.Volume, next.Chapter);
    }

    public Task<ZirusChapter?> Chapter(string seriesId, int volume, string chapter)
    {
        var url = BuildChapUrl(seriesId, volume, chapter);
        return Chapter(url);
    }

    public Task<ZirusChapter?> Chapter(string seriesId, int volume, double chapter)
    {
        return Chapter(seriesId, volume, chapter.ToString());
    }

    public Task<ZirusChapter?> Chapter(string seriesId, ZirusSeries.Next next)
    {
        return Chapter(seriesId, next.Volume, next.Chapter);
    }

    public Task<ZirusChapter?> Chapter(string url)
    {
        return _api.Get<ZirusChapter>(url);
    }
}
