using System.Reflection;
using CardboardBox.LightNovel.Core.Database;
using CardboardBox.LightNovel.Core.Sources;
using Xunit;

namespace CardboardBox.LightNovel.Core.Tests;

public class NovelCatchupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReusesBookHashesRegardlessOfCaseAndAppendsAfterTheExistingVolumeSplit(bool uppercaseHash)
    {
        var context = new Context();
        var hash = "6ab007cc2b9dbaab8851880221366732";
        var first = context.AddBook(143, "Volume 11", 11, uppercaseHash ? hash.ToUpperInvariant() : hash);
        var last = context.AddBook(152, "Volume 12", 12, "ee79a72ba2cd558136d9eef0e7309efa");
        context.AddChapter(first, 329, 1, 333);
        context.AddChapter(last, 577, 256, 589);
        var source = new Catalog(Volume("Volume 11", 329, 577, 578, 579, 598));

        Assert.Equal(3, await context.Api.CatchupBookByVolume(context.Scaffold.Series, source));
        Assert.Equal(2, context.Scaffold.Books.Length);
        Assert.Empty(context.InsertedBooks);
        Assert.Equal([152L, 152L, 152L], context.InsertedChapters.Select(c => c.BookId));
        Assert.Equal([257L, 258L, 259L], context.InsertedChapters.Select(c => c.Ordinal));
        Assert.Equal([590L, 591L, 592L], context.InsertedPages.Select(p => p.Ordinal));
        Assert.All(context.InsertedChapters, c => Assert.Matches("^[0-9a-f]{32}$", c.HashId));
        Assert.All(context.InsertedPages, p => Assert.Matches("^[0-9a-f]{32}$", p.HashId));

        var chapterIds = last.Chapters.Select(c => c.Chapter.Id).ToArray();
        Assert.Equal(0, await context.Api.CatchupBookByVolume(context.Scaffold.Series, source));
        Assert.Equal(chapterIds, last.Chapters.Select(c => c.Chapter.Id));
        Assert.Equal(3, source.DownloadedUrls.Count);
        Assert.Empty(context.InsertedBooks);
    }

    [Fact]
    public async Task UsesExistingChapterMappingsWhenTheSiteRenamesAVolume()
    {
        var context = new Context();
        var last = context.AddBook(152, "Volume 12", 12, "ee79a72ba2cd558136d9eef0e7309efa");
        context.AddChapter(last, 577, 256, 589);
        var source = new Catalog(Volume("Latest Arc", 577, 578));

        Assert.Equal(1, await context.Api.CatchupBookByVolume(context.Scaffold.Series, source));
        Assert.Empty(context.InsertedBooks);
        var chapter = Assert.Single(context.InsertedChapters);
        Assert.Equal(152, chapter.BookId);
        Assert.Equal(257, chapter.Ordinal);
    }

    [Fact]
    public async Task PlacesANewVolumeAfterExistingBooksAndKeepsItsIdentityOnTheNextRun()
    {
        var context = new Context();
        var last = context.AddBook(152, "Volume 12", 12, "ee79a72ba2cd558136d9eef0e7309efa");
        context.AddChapter(last, 577, 256, 589);
        var source = new Catalog(Volume("Volume 12", 577), Volume("Volume 13", 578));

        Assert.Equal(1, await context.Api.CatchupBookByVolume(context.Scaffold.Series, source));
        var added = Assert.Single(context.InsertedBooks);
        Assert.Equal(13, added.Ordinal);
        Assert.Matches("^[0-9a-f]{32}$", added.HashId);
        var chapter = Assert.Single(context.InsertedChapters);
        Assert.Equal(added.Id, chapter.BookId);
        Assert.Equal(1, chapter.Ordinal);
        Assert.Equal(590, Assert.Single(context.InsertedPages).Ordinal);
        Assert.Equal(0, await context.Api.CatchupBookByVolume(context.Scaffold.Series, source));
        Assert.Single(context.InsertedBooks);
    }

    private static string Url(int chapter) => $"https://example.test/novel/chapter-{chapter}/";

    private static SourceVolume Volume(string title, params int[] chapters) => new()
    {
        Title = title,
        Chapters = chapters.Select(n => new SourceChapterItem { Title = $"Chapter {n}", Url = Url(n) }).ToArray()
    };

    private sealed class Catalog(params SourceVolume[] volumes) : ISourceVolumeService
    {
        public string Name => "fixture";
        public string RootUrl => "https://example.test";
        public List<string> DownloadedUrls { get; } = [];
        public async IAsyncEnumerable<SourceVolume> Volumes(string url)
        {
            await Task.Yield();
            foreach (var volume in volumes) yield return volume;
        }
        public Task<SourceChapter?> GetChapter(string url, string title)
        {
            DownloadedUrls.Add(url);
            return Task.FromResult<SourceChapter?>(new() { Url = url, BookTitle = title, ChapterTitle = url, Content = "<p>A chapter.</p>" });
        }
        public IAsyncEnumerable<SourceChapter> Chapters(string url) => throw new NotSupportedException();
        public Task<TempSeriesInfo?> GetSeriesInfo(string url) => throw new NotSupportedException();
        public string SeriesFromChapter(string url) => throw new NotSupportedException();
    }

    private sealed class Context
    {
        public FullScaffold Scaffold { get; } = new() { Series = new() { Id = 58, Url = "https://example.test/novel/" } };
        public List<Book> InsertedBooks { get; } = [];
        public List<Chapter> InsertedChapters { get; } = [];
        public List<Page> InsertedPages { get; } = [];
        public NovelApiService Api { get; }

        public Context()
        {
            var books = Stub<IDbBookService>((method, args) =>
            {
                Assert.Equal(nameof(IDbBookService.Upsert), method.Name);
                var book = (Book)args![0]!;
                book.Id = 1000 + InsertedBooks.Count;
                InsertedBooks.Add(book);
                Scaffold.Books = [.. Scaffold.Books, new BookScaffold { Book = book }];
                return Task.FromResult(book.Id);
            });
            var chapters = Stub<IDbChapterService>((method, args) =>
            {
                Assert.Equal(nameof(IDbChapterService.Upsert), method.Name);
                var chapter = (Chapter)args![0]!;
                chapter.Id = 2000 + InsertedChapters.Count;
                InsertedChapters.Add(chapter);
                var book = Scaffold.Books.Single(b => b.Book.Id == chapter.BookId);
                book.Chapters = [.. book.Chapters, new ChapterScaffold { Chapter = chapter }];
                return Task.FromResult(chapter.Id);
            });
            var pages = Stub<IDbPageService>((method, args) =>
            {
                Assert.Equal(nameof(IDbPageService.Upsert), method.Name);
                var page = (Page)args![0]!;
                page.Id = 3000 + InsertedPages.Count;
                InsertedPages.Add(page);
                return Task.FromResult(page.Id);
            });
            var series = Stub<IDbSeriesService>((method, _) =>
            {
                Assert.Equal(nameof(IDbSeriesService.Scaffold), method.Name);
                return Task.FromResult<FullScaffold?>(Scaffold);
            });
            var maps = Stub<IDbChapterPageService>((method, args) =>
            {
                Assert.Equal(nameof(IDbChapterPageService.Upsert), method.Name);
                var map = (ChapterPage)args![0]!;
                var chapter = Scaffold.Books.SelectMany(b => b.Chapters).Single(c => c.Chapter.Id == map.ChapterId);
                chapter.Pages = [new() { Map = map, Page = InsertedPages.Single(p => p.Id == map.PageId) }];
                return Task.FromResult(4000L);
            });
            var db = new LnDbService(books, chapters, pages, series, maps);
            var constructor = typeof(NovelApiService).GetConstructors().Single();
            Api = (NovelApiService)constructor.Invoke(constructor.GetParameters()
                .Select(p => p.ParameterType == typeof(ILnDbService) ? db : (object?)null).ToArray());
        }

        public BookScaffold AddBook(long id, string title, long ordinal, string hash)
        {
            var book = new BookScaffold { Book = new() { Id = id, Title = title, Ordinal = ordinal, SeriesId = 58, HashId = hash } };
            Scaffold.Books = [.. Scaffold.Books, book];
            return book;
        }

        public void AddChapter(BookScaffold book, int number, long chapterOrdinal, long pageOrdinal)
        {
            var chapter = new Chapter { Id = number, Title = $"Chapter {number}", BookId = book.Book.Id, Ordinal = chapterOrdinal };
            var page = new Page { Id = number, Title = chapter.Title, Url = Url(number), SeriesId = 58, Ordinal = pageOrdinal };
            book.Chapters = [.. book.Chapters, new ChapterScaffold
            {
                Chapter = chapter,
                Pages = [new() { Page = page, Map = new() { ChapterId = chapter.Id, PageId = page.Id } }]
            }];
        }
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, MethodStub>();
        ((MethodStub)(object)proxy).Handler = handler;
        return proxy;
    }

    public class MethodStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!, args);
    }
}
