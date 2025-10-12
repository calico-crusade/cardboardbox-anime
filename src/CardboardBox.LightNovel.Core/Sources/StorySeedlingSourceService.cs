using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface IStorySeedlingSourceService : ISourceVolumeService { }

internal class StorySeedlingSourceService(
    IFlareSolver _flare,
    IApiService _api,
    ISmartReaderService _smart,
    ILogger<StorySeedlingSourceService> _logger) : FlareVolumeSource(_flare, _smart, _logger), IStorySeedlingSourceService
{
    public override string Name => "story-seedling";

    public override string RootUrl => "https://storyseedling.com";

    public override int MaxRequestsBeforePauseMin => 1;
    public override int MaxRequestsBeforePauseMax => 2;
    public override int PauseDurationSecondsMin => 30;
    public override int PauseDurationSecondsMax => 60;

    public int RetryCount { get; } = 10;

    //https://gist.github.com/calico-crusade/ea4944b2137dd964c384e66ff163b906
    private readonly Dictionary<char, char> _characterMap = new()
    {
        ['⽂'] = 'A',
        ['⽃'] = 'B',
        ['⽄'] = 'C',
        ['⽅'] = 'D',
        ['⽆'] = 'E',
        ['⽇'] = 'F',
        ['⽈'] = 'G',
        ['⽉'] = 'H',
        ['⽊'] = 'I',
        ['⽋'] = 'J',
        ['⽌'] = 'K',
        ['⽍'] = 'L',
        ['⽎'] = 'M',
        ['⽏'] = 'N',
        ['⽐'] = 'O',
        ['⽑'] = 'P',
        ['⽒'] = 'Q',
        ['⽓'] = 'R',
        ['⽔'] = 'S',
        ['⽕'] = 'T',
        ['⽖'] = 'U',
        ['⽗'] = 'V',
        ['⽘'] = 'W',
        ['⽙'] = 'X',
        ['⽚'] = 'Y',
        ['⽛'] = 'Z',
        ['⽜'] = 'a',
        ['⽝'] = 'b',
        ['⽞'] = 'c',
        ['⽟'] = 'd',
        ['⽠'] = 'e',
        ['⽡'] = 'f',
        ['⽢'] = 'g',
        ['⽣'] = 'h',
        ['⽤'] = 'i',
        ['⽥'] = 'j',
        ['⽦'] = 'k',
        ['⽧'] = 'l',
        ['⽨'] = 'm',
        ['⽩'] = 'n',
        ['⽪'] = 'o',
        ['⽫'] = 'p',
        ['⽬'] = 'q',
        ['⽭'] = 'r',
        ['⽮'] = 's',
        ['⽯'] = 't',
        ['⽰'] = 'u',
        ['⽱'] = 'v',
        ['⽲'] = 'w',
        ['⽳'] = 'x',
        ['⽴'] = 'y',
        ['⽵'] = 'z'
    };

    private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36 OPR/118.0.0.0";

    public string[] ParametersByMethod(string method, HtmlDocument doc, string url)
    {
        var meth = doc.DocumentNode.SelectSingleNode($"//div[starts-with(@x-data, '{method}(')]");
        if (meth is null)
        {
            _logger.LogError("Failed to get {method}: {url}", method, url);
            return [];
        }

        var data = meth.GetAttributeValue("x-data", string.Empty);
        if (string.IsNullOrEmpty(data))
        {
            _logger.LogError("Failed to get {method} data: {url}", method, url);
            return [];
        }

        data = data.Replace($"{method}(", "").Replace("\"", "").Replace("\'", "").Trim(')');
        return data.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToArray();
    }

    public (string? post, string? series) ParseTocStuff(HtmlDocument doc, string url)
    {
        var parts = ParametersByMethod("toc", doc, url);
        if (parts.Length < 2)
        {
            _logger.LogError("Failed to get toc data - split: {url}", url);
            return (null, null);
        }

        var series = parts[0];
        var post = parts[1];
        if (string.IsNullOrEmpty(series) || string.IsNullOrEmpty(post))
        {
            _logger.LogError("Failed to get toc data - empty: ({series}, {post}) {url}", series, post, url);
            return (null, null);
        }

        return (post, series);
    }

    public (string? cf, string? nonce) ParseLoadStuff(HtmlDocument doc, string url)
    {
        var parts = ParametersByMethod("loadChapter", doc, url);
        if (parts.Length < 2)
        {
            _logger.LogError("Failed to get load data - split: {url}", url);
            return (null, null);
        }

        var cf = parts[0];
        var nonce = parts[1];
        if (string.IsNullOrEmpty(cf) || string.IsNullOrEmpty(nonce))
        {
            _logger.LogError("Failed to get load data - empty: ({cf}, {nonce}) {url}", cf, nonce, url);
            return (null, null);
        }

        return (cf, nonce);
    }

    public async Task<Toc?> GetToc(string post, string series)
    {
        (string key, string value)[] items = 
        [
            ("post", post),
            ("id", series),
            ("action", "series_toc")
        ];
        return await _api.Post<Toc>($"{RootUrl}/ajax", items);
    }

    public async Task<HtmlDocument?> GetContent(string url, string? nonce)
    {
        var contentUrl = url.Trim('/') + "/content";
        var payload = new ContentRequest();

        using var response = await _api.Create(contentUrl, "POST")
            .Body(payload)
            .With(t =>
            {
                t.Headers.Add("accept", "*/*");
                t.Headers.Add("accept-language", "en-US,en;q=0.9");
                t.Headers.Add("cache-control", "no-cache");

                var cookie = Cookie;
                if (!string.IsNullOrEmpty(cookie))
                    t.Headers.Add("cookie", cookie);

                t.Headers.Add("origin", RootUrl);
                t.Headers.Add("pragma", "no-cache");
                t.Headers.Add("priority", "u=1, i");
                t.Headers.Add("referer", url);
                t.Headers.Add("sec-ch-ua", "\"Not(A:Brand\";v=\"99\", \"Opera GX\";v=\"118\", \"Chromium\";v=\"133\"");
                t.Headers.Add("sec-ch-ua-mobile", "?0");
                t.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
                t.Headers.Add("sec-fetch-dest", "empty");
                t.Headers.Add("sec-fetch-mode", "cors");
                t.Headers.Add("sec-fetch-site", "same-origin");
                t.Headers.Add("user-agent", USER_AGENT);
                t.Headers.Add("x-nonce", nonce);
            }).Result();
        var content = string.Empty;

        try
        {
            if (response is null)
                throw new NullReferenceException("Result was null");

            content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content: {url} >> {code} >> {content}", url, response?.StatusCode, content);
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(content);
        return doc;
    }

    public string Decode(string input)
    {
        var output = new StringBuilder();
        foreach (var c in input)
        {
            if (!_characterMap.TryGetValue(c, out var decoded))
            {
                output.Append(c);
                continue;
            }
                
            output.Append(decoded);
        }

        return output.ToString();
    }

    public string FilterContent(HtmlDocument doc)
    {
        var style = doc.DocumentNode.SelectSingleNode("//style");
        var styles = style?.InnerText ?? string.Empty;

        style?.Remove();

        var cssClass = new Regex("\\.(\\w+)\\s*{");

        var allClasses = cssClass.Matches(styles)
            .Select(t => t.Groups[1].Value)
            .ToArray();

        foreach (var css in allClasses)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//*[@class='{css}']");
            if (nodes is null) continue;

            foreach (var node in nodes)
                node.Remove();
        }

        var images = doc.DocumentNode.SelectNodes("//img")?.ToArray() ?? [];
        foreach(var image in images)
        {
            var src = image.GetAttributeValue("srcset", string.Empty);
            if (string.IsNullOrEmpty(src)) continue;

            var url = src.Split(' ').First();
            if (string.IsNullOrEmpty(url)) continue;

            image.SetAttributeValue("src", url);
        }

        var html = doc.DocumentNode.InnerHtml;
        var cleaned = _smart.CleanseHtml(html, RootUrl);
        return Decode(cleaned);
        //var markdown = _markdown.ToMarkdown(html);
        //var decoded = Decode(markdown);
        //return _markdown.ToHtml(decoded);
    }

    public void OpenUrl(string url)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }

    public async Task<SourceChapter?> GetChapterRaw(string url, string bookTitle, int count = 0)
    {
        await LimitCheck(CancellationToken.None);
        var doc = await Get(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get chapter: {url}", url);
            return null;
        }

        var (cf, nonce) = ParseLoadStuff(doc, url);
        if (string.IsNullOrEmpty(cf) || string.IsNullOrEmpty(nonce))
        {
            _logger.LogError("Failed to get load data: {url}", url);
            return null;
        }
        var title = doc.DocumentNode.SelectSingleNode("//h1[@class='text-xl']")?.InnerText?.HTMLDecode();
        if (string.IsNullOrEmpty(title))
        {
            _logger.LogError("Failed to get chapter title: {url}", url);
            return null;
        }

        var content = await GetContent(url, nonce);
        if (content is null)
        {
            if (count >= RetryCount) 
                throw new Exception("Failed to get content");

            int offset = count * 30;
            var delay = Random.Shared.Next(99 + offset, 130 + offset) + Random.Shared.NextDouble();
            var ts = TimeSpan.FromSeconds(delay);
            _logger.LogError("Failed to get content, retrying after {delay} seconds, try {count} of {max}: {url}", delay, count + 1, RetryCount, url);
            //OpenUrl(url);
            Limiter.Rate = 0;
            ClearCookies();
            await Task.Delay(ts);
            _logger.LogInformation("Retrying request");
            return await GetChapterRaw(url, bookTitle, count + 1);
        }

        var cleaned = FilterContent(content);
        var next = NextUrl(doc, url);
        return new SourceChapter(bookTitle, title, cleaned, next ?? string.Empty, url);
    }

    public override Task<SourceChapter?> GetChapter(string url, string bookTitle)
    {
        return GetChapterRaw(url, bookTitle);
    }

    public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        static IOrderedEnumerable<IGrouping<int, TocChapter>> DetermineVolumes(Toc toc)
        {
            var first = toc.Data.FirstOrDefault();
            if (first is null) return Array.Empty<IGrouping<int, TocChapter>>().OrderBy(t => t.Key);

            var oneVolume = !first.Slug.Contains('/');
            var notLocked = toc.Data.Where(t => !t.IsLocked);
            var grouped = oneVolume 
                ? notLocked.GroupBy(t => 1) 
                : notLocked.GroupBy(t => int.TryParse(t.Slug.Split('/').First().Trim('v'), out var res) ? res : 0);

            return grouped.OrderBy(t => t.Key);
        }

        static IEnumerable<(int volume, SourceChapterItem chapter)> DetermineChapters(Toc toc)
        {
            //keep track of whether or not the series has volumes
            bool? oneVolume = null;
            //Keep track of the volume number
            int volume = 1;
            //Iterate through the chapters
            foreach (var chapter in toc.Data)
            {
                //Skip locked chapters because they'll error out
                if (chapter.IsLocked) continue;
                //If there is no indicator of one volume or not, then figure it out
                //The slug will appear like v1/11 if it has a volume (just do this check once)
                oneVolume ??= !chapter.Slug.Contains('/');
                //Get the chapter information
                var chap = new SourceChapterItem
                {
                    Title = chapter.Title,
                    Url = chapter.Url
                };
                //If there is only volume, just add all of the chapters to it
                if (oneVolume.Value)
                {
                    yield return (volume, chap);
                    continue;
                }

                //Get the volume number from the slug
                var parts = chapter.Slug.Split('/');
                //If the volume number is valid, use it as the new volume number
                //otherwise keep the current volume number
                if (parts.Length >= 2 && 
                    int.TryParse(parts.First().Trim('v'), out var res))
                    volume = res;
                yield return (volume, chap);
            }
        }

        //Get the table of contents IDs for the series
        var (post, series) = ParseTocStuff(doc, url);
        if (string.IsNullOrEmpty(post) || string.IsNullOrEmpty(series))
        {
            _logger.LogError("Failed to get toc data - ParseVolumes: {url}", url);
            yield break;
        }

        //Get the table of contents for the series
        var toc = await GetToc(post, series);
        if (toc is null || !toc.Success)
        {
            _logger.LogError("Failed to get toc - GetToc: {url}", url);
            yield break;
        }

        //Get the grouped volumes and their chapters
        var volumes = DetermineChapters(toc)
            .GroupBy(t => t.volume)
            .OrderBy(t => t.Key);

        //Iterate through the volumes and chapters
        foreach (var volume in volumes)
        {
            //Determine the title of the volume
            var title = "Volume " + volume.Key;
            //Get the chapters for the volume
            var chapters = volume.Select(t => t.chapter).ToArray();
            //Return the volume
            yield return new SourceVolume
            {
                Chapters = chapters,
                Title = title,
                Forwards = [],
                Inserts = [],
                Url = url
            };
        }
    }

    public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await Get(url);
        if (doc is null)
        {
            _logger.LogError("Failed to get series info: {url}", url);
            return null;
        }

        var title = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", string.Empty);
        if (string.IsNullOrEmpty(title))
        {
            _logger.LogError("Failed to get series title: {url}", url);
            return null;
        }

        var image = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", string.Empty);
        var desc = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']")?.GetAttributeValue("content", string.Empty);

        var genres = doc.DocumentNode
            .SelectNodes("//a[starts-with(@href, 'https://storyseedling.com/browse/?includeGenres')]")?
            .Select(t => t.InnerText?.HTMLDecode())
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!)
            .ToArray() ?? [];
        var tags = doc.DocumentNode
            .SelectNodes("//a[starts-with(@href, 'https://storyseedling.com/browse/?includeTags')]")?
            .Select(t => t.InnerText?.HTMLDecode())
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!.Trim('#'))
            .ToArray() ?? [];

        var volumes = await GetVolumes(url, doc);
        var firstUrl = volumes.FirstOrDefault()?.Chapters.FirstOrDefault()?.Url;

        return new TempSeriesInfo(title, desc, [], image, firstUrl, genres, tags);
    }

    public override string SeriesFromChapter(string url)
    {
        var trim = url.ToLower().Replace(RootUrl, "").Replace("series", "").Trim('/');
        var part = trim.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (part.Length < 2) throw new Exception("Could not find series from chapter");

        return $"{RootUrl.TrimEnd('/')}/series/{part.First()}";
    }

    public override string? NextUrl(HtmlDocument doc, string url)
    {
        var next = doc.DocumentNode.SelectNodes("//small[@class='text-sm leading-none sr-only']");
        if (next is null) return null;

        foreach(var node in next)
        {
            if (node.InnerText.ToLower() != "next") continue;

            var parent = node.ParentNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(parent)) continue;

            return parent.HTMLDecode();
        }

        return null;
    }

    public record class TocChapter(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("slug")] string Slug,
        [property: JsonPropertyName("is_locked")] bool IsLocked,
        [property: JsonPropertyName("is_read")] bool IsRead,
        [property: JsonPropertyName("price")] string Price,
        [property: JsonPropertyName("bought")] bool Bought,
        [property: JsonPropertyName("date")] string Date);

    public record class Toc(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("data")] TocChapter[] Data);

    public record class ContentRequest(
        [property: JsonPropertyName("captcha_response")] string Response = "");
}
