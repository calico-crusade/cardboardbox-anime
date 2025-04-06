namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface IRoyalRoadSourceService : ISourceVolumeService { }

internal class RoyalRoadSourceService(
    IFlareSolver _flare,
    ISmartReaderService _reader,
    ILogger<RoyalRoadSourceService> _logger) : RatedSource, IRoyalRoadSourceService
{
    private SolverCookie[]? _cookies = null;

    public string Name => "royal-road";

    public string RootUrl => "https://www.royalroad.com";

    public override int MaxRequestsBeforePauseMin => 10;
    public override int MaxRequestsBeforePauseMax => 20;
    public override int PauseDurationSecondsMin => 15;
    public override int PauseDurationSecondsMax => 35;

    private readonly Dictionary<string, SourceVolume[]> _volumes = [];

    public SourceVolume[] GetVolumes(string url, HtmlDocument doc)
    {
        url = url.Trim('/').ToLower();

        if (_volumes.TryGetValue(url, out var volumes))
            return volumes;

        return _volumes[url] = ParseVolumes(doc, url).ToArray();
    }

    public IEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        RoyalRoadChapter[] ParseChapters(string text)
        {
            try
            {
                var items = JsonSerializer.Deserialize<RoyalRoadChapter[]>(text)
                    ?? throw new Exception("Failed to parse chapters");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse chapters: {url}", url);
                return [];
            }
        }

        RoyalRoadVolume[] ParseVolumes(string text)
        {
            try
            {
                var items = JsonSerializer.Deserialize<RoyalRoadVolume[]>(text)
                    ?? throw new Exception("Failed to parse volumes");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse volumes: {url}", url);
                return [];
            }
        }

        IEnumerable<SourceVolume> SortVolumes(RoyalRoadVolume[] volumes, RoyalRoadChapter[] chapters, string cover)
        {
            SourceChapterItem Convert(RoyalRoadChapter chapter)
            {
                var url = _reader.FixUrl(chapter.Url, RootUrl);
                return new SourceChapterItem
                {
                    Title = chapter.Title,
                    Url = url
                };
            }

            if (volumes.Length == 0)
            {
                yield return new SourceVolume
                {
                    Title = "Volume 1",
                    Url = url,
                    Chapters = chapters
                        .OrderBy(t => t.Order)
                        .Select(Convert)
                        .ToArray(),
                    Inserts = [cover],
                    Forwards = [cover],
                };
                yield break;
            }

            var sorted = volumes.OrderBy(t => t.Order).ToArray();

            for (var i = 0; i < volumes.Length; i++)
            {
                var isLast = i == volumes.Length - 1;
                var volume = volumes[i];
                var chaps = chapters
                    .Where(t => t.VolumeId == volume.Id || (isLast && t.VolumeId is null))
                    .OrderBy(t => t.Order)
                    .Select(Convert)
                    .ToArray();

                if (chaps.Length == 0) continue;

                yield return new SourceVolume
                {
                    Title = volume.Title,
                    Url = url,
                    Chapters = chaps,
                    Inserts = [volume.Cover, cover],
                    Forwards = [volume.Cover, cover],
                };
            }
        }

        var scripts = doc.DocumentNode.SelectNodes("//script");
        if (scripts is null)
        {
            _logger.LogError("Failed to get volumes");
            yield break;
        }

        var coverRegex = new Regex("window.fictionCover = \"(.*?)\";", RegexOptions.Compiled);
        var chaptersRegex = new Regex("window.chapters = \\[(.*?)\\];", RegexOptions.Compiled | RegexOptions.Multiline);
        var volumesRegex = new Regex("window.volumes = \\[(.*?)\\];", RegexOptions.Compiled | RegexOptions.Multiline);

        foreach (var script in scripts)
        {
            var text = script.InnerText;
            if (string.IsNullOrEmpty(text)) continue;

            var coverMatch = coverRegex.Match(text);
            if (!coverMatch.Success) continue;

            var coverUrl = coverMatch.Groups[1].Value;

            var chaptersMatch = chaptersRegex.Match(text);
            if (!chaptersMatch.Success) continue;

            var chaptersJson = "[" + chaptersMatch.Groups[1].Value + "]";
            var chapters = ParseChapters(chaptersJson);
            if (chapters.Length == 0) continue;

            var volumesMatch = volumesRegex.Match(text);
            var volumes = volumesMatch.Success
                ? ParseVolumes("[" + volumesMatch.Groups[1].Value + "]")
                : [];

            foreach (var volume in SortVolumes(volumes, chapters, coverUrl))
                yield return volume;
            yield break;
        }

        _logger.LogInformation("Could not find chapters for: {url}", url);
    }

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

    public Task<HtmlDocument> Get(string url)
    {
        return DoRequest(url);
    }

    public IAsyncEnumerable<SourceChapter> Chapters(string firstUrl)
    {
        static string Safe(string url) => url.Trim('/').Trim().ToLower();

        var safeUrl = Safe(firstUrl);
        var series = SeriesFromChapter(firstUrl);
        var volumes = Volumes(series)
            .SelectMany(volume => volume.Chapters.Select(chapter => (volume, chapter)).ToAsyncEnumerable())
            .SkipWhile(t => Safe(t.chapter.Url) != safeUrl);

        string url = firstUrl;

        var limiter = CreateLimiter<(SourceVolume volume, SourceChapterItem chapter), SourceChapter?>(item =>
        {
            return GetChapter(item.chapter.Url, item.volume.Title);
        });

        var token = CancellationToken.None;
        return limiter.Fetch(volumes, _logger, token)
            .Where(t => t is not null)
            .Select(t => t!);
    }

    public async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await Get(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return null;
        }

        var dataNode = doc.DocumentNode.SelectSingleNode("//script[@type='application/ld+json']");
        if (dataNode is null)
        {
            _logger.LogError("Failed to get series info - No LD JSON: {url}", url);
            return null;
        }

        var text = dataNode.InnerText;
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogError("Failed to get series info - Empty LD JSON: {url}", url);
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<RoyalRoadLdJson>(text);
            if (data is null)
            {
                _logger.LogError("Failed to parse series info - Empty LD JSON: {url}", url);
                return null;
            }

            var author = data.Author?.Name ?? string.Empty;
            var title = data.Name;
            var description = data.Description;
            var image = data.Image;
            var genres = data.Genre
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            var volumes = GetVolumes(url, doc);
            var firstChap = volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url;
            return new TempSeriesInfo(title, description, [author], image, firstChap, genres, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse series info: {url}", url);
            return null;
        }
    }

    public string SeriesFromChapter(string url)
    {
        var trim = url.ToLower().Replace(RootUrl, "").Trim('/');
        var part = trim.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (part.Length < 3) throw new Exception("Could not find series from chapter");

        var series = string.Join('/', part[..3]);
        return $"{RootUrl.TrimEnd('/')}/{series}";
    }

    public async IAsyncEnumerable<SourceVolume> Volumes(string url)
    {
        var safeSeriesUrl = url.Trim('/').ToLower();
        if (_volumes.TryGetValue(safeSeriesUrl, out var volumes))
        {
            foreach (var volume in volumes)
                yield return volume;
            yield break;
        }

        var doc = await Get(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            yield break;
        }

        volumes = GetVolumes(url, doc);
        foreach (var volume in volumes)
            yield return volume;
    }

    public (string? title, string? content) BackupParse(HtmlDocument document, string url)
    {
        var contentNode = document.DocumentNode.SelectSingleNode("//div[@class='chapter-inner chapter-content']");
        if (contentNode is null)
        {
            _logger.LogError("Failed to get series content: {url}", url);
            return (null, null);
        }

        var title = document.DocumentNode
            .SelectSingleNode("//meta[@name='twitter:title']")?
            .GetAttributeValue("content", string.Empty);
        var clean = _reader.CleanseHtml(contentNode.InnerHtml, url.GetRootUrl());
        return (title, clean);
    }

    public async Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        var doc = await Get(url);
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

        title = string.Join('-', title
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .SkipLast(1) ?? []);

        var nextUrl = doc.DocumentNode
            .SelectNodes("//a[@class='btn btn-primary col-xs-12']")?
            .FirstOrDefault(t =>
            {
                if (t is null) return false;
                var text = t.InnerText?.Trim().ToLower();
                if (text is null) return false;
                return text.Contains("next") && text.Contains("chapter");
            })?
            .GetAttributeValue("href", string.Empty);

        if (!string.IsNullOrEmpty(nextUrl))
            nextUrl = _reader.FixUrl(nextUrl, RootUrl);

        return new SourceChapter(bookTitle, title, content, nextUrl ?? string.Empty, url);
    }

    internal class RoyalRoadLdJson
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("genre")]
        public string[] Genre { get; set; } = [];

        [JsonPropertyName("author")]
        public RoyalRoadLdJsonAuthor? Author { get; set; }
    }

    internal class RoyalRoadLdJsonAuthor
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    internal class RoyalRoadChapter
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("volumeId")]
        public long? VolumeId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("visible")]
        public int Visible { get; set; }

        [JsonPropertyName("isUnlocked")]
        public bool IsUnlocked { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    internal class RoyalRoadVolume
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("cover")]
        public string Cover { get; set; } = string.Empty;

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }
}
