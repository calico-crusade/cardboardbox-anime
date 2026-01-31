namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public abstract class FlareVolumeSource(
    IFlareSolver _flare,
    ISmartReaderService _reader,
    ILogger _logger) : ISourceVolumeService
{
    private SolverCookie[]? _cookies = null;
    private RateLimiterBase? _rateLimiter = null;
    private (int limit, int timeout)? _limiter = null;
    private readonly Dictionary<string, SourceVolume[]> _volumes = [];
    private readonly Dictionary<string, HtmlDocument> _pageCache = new(StringComparer.InvariantCultureIgnoreCase);

    public abstract string Name { get; }

    public abstract string RootUrl { get; }

    public virtual int MaxRequestsBeforePauseMin => 2;
    public virtual int MaxRequestsBeforePauseMax => 6;
    public virtual int PauseDurationSecondsMin => 15;
    public virtual int PauseDurationSecondsMax => 35;
    public virtual int MaxRetries => 4;

    public virtual RateLimiterBase Limiter => _rateLimiter ??= new(
        new(MaxRequestsBeforePauseMin, MaxRequestsBeforePauseMax),
        new(PauseDurationSecondsMin, PauseDurationSecondsMax));

    public string Cookie => _cookies is null
        ? string.Empty
        : string.Join("; ", _cookies.Select(c => $"{c.Name}={c.Value}"));

    public void ClearCookies()
    {
        _cookies = null;
    }

    public void SetCookie(string key, string value)
    {
        _cookies ??= [];

        var cookie = _cookies.FirstOrDefault(c => c.Name == key);
        if (cookie is not null)
        {
            cookie.Value = value;
            return;
        }
        
        _cookies = [.. _cookies.Append(new SolverCookie(key, value))];
    }

    public async Task LimitCheck(CancellationToken token)
    {
        if (!Limiter.Enabled || token.IsCancellationRequested) return;

        var (limit, timeout) = _limiter ??= Limiter.GetRateLimit();

        if (Limiter.Rate < limit)
        {
            _logger.LogInformation("Below rate limit. Count: {count} - {rate}/{limit} - {timeout}ms",
                Limiter.Count, Limiter.Rate, limit, timeout);
            Limiter.Count++;
            Limiter.Rate++;
            return;
        }

        _logger.LogInformation("Rate limit reached. Pausing for {timeout}ms. Count: {count} - {rate}/{limit}",
            timeout, Limiter.Count, Limiter.Rate, limit);

        await Task.Delay(timeout, token);
        Limiter.Rate = 0;
        ClearCookies();
        _limiter = Limiter.GetRateLimit();
        _logger.LogInformation("Resuming after pause. New Limits {limit} - {timeout}ms", limit, timeout);
    }

    public virtual async Task<SourceVolume[]> GetVolumes(string url, HtmlDocument doc)
    {
        url = url.Trim('/').ToLower();

        if (_volumes.TryGetValue(url, out var volumes))
            return volumes;

        return _volumes[url] = await ParseVolumes(doc, url).ToArrayA();
    }

    private async Task<HtmlDocument> DoRequest(string url, int count = 0)
    {
        try
        {
            _logger.LogInformation("Getting data from {url}", url);
            var data = await _flare.Get(url, _cookies, timeout: 30_000);
            if (data is null || data.Solution is null) throw new Exception("Failed to get data");

            if (data.Solution.Status < 200 || data.Solution.Status >= 300)
                throw new Exception($"Failed to get data: {data.Solution.Status}");

            _cookies = data.Solution.Cookies;

            var doc = new HtmlDocument();
            doc.LoadHtml(data.Solution.Response);
            _logger.LogInformation("Got data from {url}", url);
            return doc;
        }
        catch (Exception ex)
        {
            if (count > MaxRetries) throw;

            count++;
            ClearCookies();
            var delay = Random.Shared.Next(PauseDurationSecondsMin, PauseDurationSecondsMax);
            _logger.LogError(ex, "Failed to get data for url {count}/{max}, retrying after {delay} seconds: {url}", count, MaxRetries, delay, url);
            await Task.Delay(delay * 1000);
            _logger.LogInformation("Retrying request");
            return await DoRequest(url, count);
        }
    }

    public virtual async Task<HtmlDocument> Get(string url, bool cache = false)
    {
        if (_pageCache.TryGetValue(url, out var doc))
            return doc;

        var page = await DoRequest(url);
        if (cache) _pageCache[url] = page;
        return page;
    }

    public virtual async IAsyncEnumerable<SourceVolume> Volumes(string url)
    {
        var safeSeriesUrl = url.Trim('/').ToLower();
        if (_volumes.TryGetValue(safeSeriesUrl, out var volumes))
        {
            foreach (var volume in volumes)
                yield return volume;
            yield break;
        }

        var doc = await Get(url, true);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            yield break;
        }

        volumes = await GetVolumes(url, doc);
        foreach (var volume in volumes)
            yield return volume;
    }

    public virtual async IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        static string Safe(string url) => url.Trim('/').Trim().ToLower();

        var safeUrl = Safe(firstUrl);
        var series = SeriesFromChapter(firstUrl);
        var volumes = Volumes(series)
            .SelectMany(volume => volume.Chapters.Select(chapter => (volume, chapter)))
            .SkipWhileA(t => Safe(t.chapter.Url) != safeUrl);

        await foreach(var item in volumes)
        {
            var chapter = await GetChapter(item.chapter.Url, item.volume.Title);
            if (chapter is null) continue;

            yield return chapter;
        }
    }

    public virtual async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        await LimitCheck(CancellationToken.None);

        var doc = await Get(url, true);
        if (doc is null)
        {
            _logger.LogError("Failed to get chapter: {url}", url);
            return null;
        }

        var (title, content) = await _reader.GetCleanArticle(doc, url);
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            var (nt, nc) = BackupParse(doc, url);
            title = nt;
            content = nc;
        }

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            _logger.LogError("Failed to get chapter: {url}", url);
            return null;
        }

        title = MassageTitle(title);
        var nextUrl = NextUrl(doc, url);

        if (!string.IsNullOrEmpty(nextUrl))
            nextUrl = _reader.FixUrl(nextUrl, RootUrl);

        return new SourceChapter(bookTitle, title, content, nextUrl ?? string.Empty, url);
    }

    public virtual (string? title, string? content) BackupParse(HtmlDocument doc, string url)
    {
        return (null, null);
    }

    public virtual string MassageTitle(string title)
    {
        return title;
    }

    public abstract string SeriesFromChapter(string url);

    public abstract string? NextUrl(HtmlDocument doc, string url);

    public abstract IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url);

    public abstract Task<TempSeriesInfo?> GetSeriesInfo(string url);
}
