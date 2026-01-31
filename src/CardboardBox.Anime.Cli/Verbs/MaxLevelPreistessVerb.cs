using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

using CardboardBox.Extensions;
using LightNovel.Core;
using LightNovel.Core.Sources;
using LightNovel.Core.Sources.Utilities;

[Verb("max-level-preistess", HelpText = "Runs the max level preistess light novel updater")]
public class MaxLevelPreistessOptions
{

}

internal partial class MaxLevelPreistessVerb(
    ILnDbService _db,
    ISmartReaderService _smart,
    ILogger<MaxLevelPreistessVerb> logger,
    ICardboardTranslationsSourceService _ctl) : BooleanVerb<MaxLevelPreistessOptions>(logger)
{
    private const long SERIES_ID = 139;
    private const string SERIES_TITLE = "I Am a Max-level Priestess in Another World";

    public CardboardTranslationsSourceService Ctl => (_ctl as CardboardTranslationsSourceService)!;

    public async IAsyncEnumerable<MLPChapter> FetchChapters()
    {
        var regex = TitleRegex();

        var chapters = Ctl.AllEntries(SERIES_TITLE);
        await foreach (var entry in chapters)
        {
            var title = entry.Title.Text.HTMLDecode();
            var match = regex.Match(title);
            if (!match.Success)
            {
                _logger.LogInformation("Title didn't match regex: {title}", title);
                continue;
            }

            if (!int.TryParse(match.Groups[1].Value, out var volume))
            {
                _logger.LogInformation("Volume number wasn't an integer: {vol} - {title}", 
                    match.Groups[1].Value, title);
                continue;
            }

            if (!double.TryParse(match.Groups[2].Value, out var chapter))
            {
                _logger.LogInformation("Chapter number wasn't a double: {chap} - {title}", 
                    match.Groups[2].Value, title);
            }

            var url = entry.Links.FirstOrDefault(t => t.Rel == "alternate")?.Href ?? string.Empty;
            var content = entry.Content.Text.HTMLDecode().Trim();
            content = _smart.CleanseHtml(content, Ctl.RootUrl);
            yield return new(volume, chapter, title, url, content);
        }
    }

    public async IAsyncEnumerable<MLPChapter> SortNextChapter(IAsyncEnumerable<MLPChapter> chapters)
    {
        MLPChapter? last = null;
        await foreach(var chap in chapters)
        {
            if (last is not null)
                last.NextUrl = chap.Url;
            yield return last = chap;
        }
        if (last is not null)
            yield return last;
    }

    public static IEnumerable<SeriesUnnest> Unnest(FullScaffold scaffold)
    {
        var series = scaffold.Series;
        foreach(var book in scaffold.Books)
        foreach(var chap in book.Chapters)
        foreach(var page in chap.Pages)
            yield return new SeriesUnnest(series, book.Book, chap.Chapter, page.Page, page.Map);
    }

    public override async Task<bool> Execute(MaxLevelPreistessOptions options, CancellationToken token)
    {
        var series = await _db.Series.Scaffold(SERIES_ID);
        if (series is null)
        {
            var s = new Series
            {
                Title = SERIES_TITLE,
                Authors = ["Cardboard"],
                Description = @"In the fully immersive and realistic RPG online game [Illusory World], which has been in official public beta testing for its fourth year, Violet, one of the top players, experiences an unpredictable twist while participating in the conquest of a special dungeon. <br>
With her eyes closed and then opened, Violet finds herself no longer in the game world but in a completely new and unknown place, separated from the modern society behind it. <br>
Here, there is no doubt that it is the real world. <br>
Violet’s incredible adventure begins, but this world seems somewhat different from what she had expected. <br>
Enemy A: Why? You’re just a priestess, aren’t you!? <br>
Violet: Is there a problem between me being a priestess and me being able to beat you with a stick? Drop your loot and hand it over!",
                Editors = ["Cardboard"],
                Genre = ["Action", "Adventure", "Gender Bender"],
                Illustrators = ["Cardboard"],
                Image = "https://fanstranslations.com/wp-content/uploads/2023/12/11185e5f-36b5-494f-b904-8ca3e95e5645.jpg",
                Tags = ["Isekai", "Web Novel", "Fantasy"],
                Url = "https://www.cardboardtranslation.com/2025/01/im-max-level-priestess-in-another-world.html",
                HashId = SERIES_TITLE.MD5Hash(),
                Translators = ["Cardboard"],
            };
            _logger.LogWarning("Couldn't find series {id} in the database! Creating it!", SERIES_ID);
            s.Id = await _db.Series.Upsert(s);
            series = await _db.Series.Scaffold(s.Id);
            _logger.LogInformation("Created series {id}", s.Id);

            if (series is null)
            {
                _logger.LogError("Failed to create series {id} in the database!", s.Id);
                return false;
            }
        }

        var image = series.Series.Image!;
        var loaded = new HashSet<string>();
        var books = series.Books.Select(t => t.Book).ToDictionary(t => t.Ordinal);
        var unnest = Unnest(series).ToDictionary(t => t.Page.HashId);
        var chapters = SortNextChapter(FetchChapters()).OrderBy(t => t.Volume).ThenBy(t => t.Ordinal);
        await foreach(var chapter in chapters)
        {
            if (unnest.ContainsKey(chapter.PageHash))
            {
                _logger.LogInformation("Chapter already exists: {title}", chapter.Title);
                continue;
            }

            if (loaded.Contains(chapter.ChapterHash))
            {
                _logger.LogInformation("Chapter already loaded: {title}", chapter.Title);
                continue;
            }

            if (!books.TryGetValue(chapter.Volume, out var book))
            {
                var title = $"{SERIES_TITLE} - Volume {chapter.Volume}";
                book = new Book
                {
                    SeriesId = series.Series.Id,
                    CoverImage = image,
                    Forwards = [image],
                    Inserts = [image],
                    Title = title,
                    HashId = title.MD5Hash(),
                    Ordinal = chapter.Volume,
                };
                book.Id = await _db.Books.Upsert(book);
                books[book.Ordinal] = book;
            }

            var chap = new Chapter
            {
                HashId = chapter.ChapterHash,
                Title = chapter.Title,
                Ordinal = chapter.Ordinal,
                BookId = book.Id,
            };
            chap.Id = await _db.Chapters.Upsert(chap);

            var lastOrdinal = unnest.Count == 0 ? 1 : unnest.Values.Max(t => t.Page.Ordinal) + 1;

            var page = new Page
            {
                HashId = chapter.PageHash,
                Title = chapter.Title,
                Ordinal = lastOrdinal,
                SeriesId = series.Series.Id,
                Url = chapter.Url,
                NextUrl = chapter.NextUrl,
                Content = chapter.Content,
                Mimetype = "application/html"
            };
            page.Id = await _db.Pages.Upsert(page);

            var cp = new ChapterPage
            {
                ChapterId = chap.Id,
                PageId = page.Id,
                Ordinal = 0
            };
            cp.Id = await _db.ChapterPages.Upsert(cp);

            var additive = new SeriesUnnest(series.Series, book, chap, page, cp);
            unnest[page.HashId] = additive;
            _logger.LogInformation("Loaded chapter: {title}", chapter.Title);
            loaded.Add(chapter.ChapterHash);
        }

        _logger.LogInformation("Finished");
        return true;
    }

    public record class MLPChapter(
        int Volume,
        double Chapter,
        string Title,
        string Url,
        string Content)
    {
        public string PageHash => Url.MD5Hash();

        public int Ordinal => (int)Chapter;

        public string ChapterHash => $"{Title}-{Ordinal}".MD5Hash();

        public string? NextUrl { get; set; }
    }

    public record class SeriesUnnest(
        Series Series,
        Book Book,
        Chapter Chapter,
        Page Page,
        ChapterPage ChapterPage);

    [GeneratedRegex("\\[Vol\\. ([0-9]{1,})\\] Chapter ([0-9,\\.]+)(:?) (.*)")]
    private static partial Regex TitleRegex();
}
