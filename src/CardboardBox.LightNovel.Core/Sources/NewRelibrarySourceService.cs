namespace CardboardBox.LightNovel.Core.Sources;

using Utilities;
using Utilities.FlareSolver;

public interface INewRelibrarySourceService : ISourceVolumeService { }

internal class NewRelibrarySourceService(
    IFlareSolver _flare,
    ISmartReaderService _smart,
    INovelUpdatesService _info,
    ILogger<NewRelibrarySourceService> _logger) : FlareVolumeSource(_flare, _smart, _logger), INewRelibrarySourceService
{

    public override string Name => "relib";
    public override string RootUrl => "https://re-library.com";
    public override int MaxRequestsBeforePauseMin => 1;
    public override int MaxRequestsBeforePauseMax => 5;
    public override int PauseDurationSecondsMin => 30;
    public override int PauseDurationSecondsMax => 60;

    private readonly Dictionary<string, string> _nulRedirect = new(StringComparer.InvariantCultureIgnoreCase)
    {
        ["https://re-library.com/translations/alchemist-startover"] = "https://www.novelupdates.com/series/alchemist-startover"
    };

    public override async Task<TempSeriesInfo?> GetSeriesInfo(string url)
    {
        var doc = await Get(url, true);
        if (doc is null) return null;

        var nul = doc.Attribute("//a[starts-with(@href, 'https://www.novelupdates.com/series/')]", "href");
        if (string.IsNullOrWhiteSpace(nul) && 
            !_nulRedirect.TryGetValue(url.Trim('/'), out nul)) 
            return null;

        var series = await _info.Series(nul);
        if (series == null) return null;

        var fc = doc.Attribute("//div[@class='su-spoiler-content su-u-clearfix su-u-trim']/ul[@class='rl-subpages']/li/a", "href");

        return new TempSeriesInfo(series.Title, series.Description, series.Authors, series.Image, fc, series.Genre, series.Tags);
    }

    public override string? NextUrl(HtmlDocument doc, string url)
    {
        return doc.Attribute("//div[@class='nextPageLink PageLink']/a", "href");
    }

    public override async IAsyncEnumerable<SourceVolume> ParseVolumes(HtmlDocument doc, string url)
    {
        var links = doc.DocumentNode.SelectNodes("//div[@class='su-spoiler-content su-u-clearfix su-u-trim']/ul[@class='rl-subpages']/li/a");
        if (links is null) yield break;

        var converted = links.Select<HtmlNode, (string volume, SourceChapterItem? chap)>(x =>
        {
            var href = x.GetAttributeValue("href", string.Empty);
            var regex = new Regex("https://re-library.com/translations/(.*?)/(.*?)/(.*?)");
            var match = regex.Match(href);
            if (!match.Success) return (string.Empty, null);

            var volume = match.Groups[2].Value
                .Replace("-", " ")
                .Replace("volume", "", StringComparison.InvariantCultureIgnoreCase)
                .Trim();
            if (!int.TryParse(volume, out var volNum))
                volNum = 0;

            var innerHtml = x.InnerText?.HTMLDecode()?.Trim();
            if (innerHtml is null ||
                innerHtml.Contains("(Unlocks on ", StringComparison.InvariantCultureIgnoreCase))
                return (string.Empty, null);

            var chapter = new SourceChapterItem
            {
                Title = innerHtml,
                Url = href
            };
            return (volume, chapter);
        }).Where(t => t.chap is not null);

        var volume = string.Empty;
        var chapters = new List<SourceChapterItem>();
        foreach (var (vol, chap) in converted)
        {
            if (vol != volume && chapters.Count > 0)
            {
                var volTitle = volume == string.Empty ? "Miscellaneous" : $"Volume {volume}";
                yield return new SourceVolume
                {
                    Title = volTitle,
                    Url = url,
                    Chapters = [..chapters]
                };
                chapters.Clear();
            }

            volume = vol;
            if (chap is not null)
                chapters.Add(chap);
        }

        if (chapters.Count > 0)
        {
            var volTitle = volume == string.Empty ? "Miscellaneous" : $"Volume {volume}";
            yield return new SourceVolume
            {
                Title = volTitle,
                Url = url,
                Chapters = [..chapters]
            };
        }
        //.GroupBy(t => t.volume)
        //.OrderBy(t => t.Key)
        //.Select(t =>
        //{
        //    var volTitle = t.Key == 0 ? "Miscellaneous" : $"Volume {t.Key}";
        //    return new SourceVolume
        //    {
        //        Title = volTitle,
        //        Url = url,
        //        Chapters = [..t.Select(x => x.chap!)]
        //    };
        //});

        //foreach (var volume in converted)
        //    yield return volume;
    }

    public override string SeriesFromChapter(string url)
    {
        var regex = new Regex("https://re-library.com/translations/(.*?)/(.*?)/(.*?)");
        var match = regex.Match(url);
        if (!match.Success) return "";

        return $"https://re-library.com/translations/{match.Groups[1].Value}/";
    }
}
