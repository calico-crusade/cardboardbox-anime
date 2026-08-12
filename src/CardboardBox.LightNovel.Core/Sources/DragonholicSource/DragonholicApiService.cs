namespace CardboardBox.LightNovel.Core.Sources.DragonholicSource;

public interface IDragonholicApiService
{
    Task<DragonholicSeries?> Series(string slug);

    Task<DragonholicChapterItem[]> Chapters(long seriesId);

    Task<DragonholicChapter?> Chapter(long chapterId);
}

public class DragonholicApiService(IApiService _api) : IDragonholicApiService
{
    public const string RootUrl = "https://dragonholictranslations.com";
    public const string WordPressApiUrl = $"{RootUrl}/wp-json/wp/v2";
    public const string ChapterCatalogUrl = $"{RootUrl}/api/chapters";

    private readonly Dictionary<string, Task<DragonholicSeries?>> _series = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, Task<DragonholicChapterItem[]>> _chapters = [];
    private readonly Dictionary<long, Task<DragonholicChapter?>> _chapter = [];

    public Task<DragonholicSeries?> Series(string slug)
    {
        if (_series.TryGetValue(slug, out var series))
            return series;

        return _series[slug] = FetchSeries(slug);
    }

    private async Task<DragonholicSeries?> FetchSeries(string slug)
    {
        var safeSlug = Uri.EscapeDataString(slug);
        var url = $"{WordPressApiUrl}/series?slug={safeSlug}&_embed=wp:featuredmedia,wp:term";
        var response = await _api.Get<DragonholicSeries[]>(url);
        return response?.FirstOrDefault();
    }

    public Task<DragonholicChapterItem[]> Chapters(long seriesId)
    {
        if (_chapters.TryGetValue(seriesId, out var chapters))
            return chapters;

        return _chapters[seriesId] = FetchChapters(seriesId);
    }

    private async Task<DragonholicChapterItem[]> FetchChapters(long seriesId)
    {
        var url = $"{ChapterCatalogUrl}?series_id={seriesId}&sort_order=asc&load_all=1";
        var response = await _api.Get<DragonholicChapterCatalog>(url);
        if (response is null || !response.Success)
            return [];

        var unlocked = response.UnlockedChapterIds.ToHashSet();
        return response.Chapters
            .Where(chapter => !chapter.IsPremium || unlocked.Contains(chapter.Id))
            .OrderBy(chapter => chapter.ChapterOrder)
            .ThenBy(chapter => chapter.Id)
            .ToArray();
    }

    public Task<DragonholicChapter?> Chapter(long chapterId)
    {
        if (_chapter.TryGetValue(chapterId, out var chapter))
            return chapter;

        return _chapter[chapterId] = _api.Get<DragonholicChapter>(
            $"{WordPressApiUrl}/chapter/{chapterId}?_fields=id,parent,slug,link,title,content");
    }
}
