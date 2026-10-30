using System.Net;
using System.Text;
using System.Text.Json;
using CardboardBox.Http;
using CardboardBox.Json;
using CardboardBox.LightNovel.Core.Sources.DragonholicSource;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CardboardBox.LightNovel.Core.Tests;

public class DragonholicSourceTests
{
    private const string Root = "https://dragonholictranslations.com";
    private const string SeriesUrl = Root + "/series/example/";
    private const string SeriesApi = Root + "/wp-json/wp/v2/series?slug=example&_embed=wp:featuredmedia,wp:term";
    private const string CatalogApi = Root + "/api/chapters?series_id=100&sort_order=asc&load_all=1";
    private const string SeriesJson = """
        [{"id":100,"slug":"example","link":"https://dragonholictranslations.com/series/example/",
          "title":{"rendered":"A Witch &amp; Bear"},"content":{"rendered":"<p>A fantasy story.</p>"},
          "_embedded":{"wp:featuredmedia":[{"source_url":"https://dragonholictranslations.com/cover.webp"}],
          "wp:term":[[{"name":"An Author","taxonomy":"series-author"}],
            [{"name":"Fantasy","taxonomy":"genre"}], [{"name":"Magic &amp; Potions","taxonomy":"series-tag"}]]}}]
        """;

    [Fact]
    public async Task ReadsSeriesMetadataAndCachesRequests()
    {
        using var context = new Context("""[{"id":"1","slug":"chapter-1","heading":"Chapter 1","chapter_order":"1"}]""");
        var info = await context.Source.GetSeriesInfo(SeriesUrl);
        Assert.NotNull(info);
        Assert.Equal("A Witch & Bear", info.Title);
        Assert.Equal("<p>A fantasy story.</p>", info.Description);
        Assert.Equal(["An Author"], info.Authors);
        Assert.Equal(["Fantasy"], info.Genre);
        Assert.Equal(["Magic & Potions"], info.Tags);
        Assert.Equal(Root + "/cover.webp", info.Image);
        Assert.Equal(SeriesUrl + "chapter-1/", info.FirstChapterUrl);
        var volumes = await context.Source.Volumes(SeriesUrl).ToArrayA();
        Assert.Equal("Volume 1", Assert.Single(volumes).Title);
        Assert.Equal([SeriesApi, CatalogApi], context.Http.Requests);
    }

    [Fact]
    public async Task SortsFractionalOrdersAndOnlyIncludesAccessibleChapters()
    {
        using var context = new Context("""
            [{"id":"5","slug":"locked","heading":"Chapter 3","chapter_order":"3","is_premium":true},
             {"id":"3","slug":"owned","heading":"Chapter 2","chapter_order":"2","is_premium":true},
             {"id":"2","slug":"part","heading":"Chapter 1.5","chapter_order":"1.5","is_premium":false},
             {"id":"1","slug":"first","heading":"Chapter &amp; One","subtitle":"A beginning","chapter_order":"1"}]
            """, "[\"3\"]");
        var volume = Assert.Single(await context.Source.Volumes(SeriesUrl).ToArrayA());
        Assert.Equal([SeriesUrl + "first/", SeriesUrl + "part/", SeriesUrl + "owned/"], volume.Chapters.Select(c => c.Url));
        Assert.Equal("Chapter & One: A beginning", volume.Chapters[0].Title);
        Assert.Equal(3, (await context.Api.Chapters(100)).Length);
    }

    [Fact]
    public async Task KeepsMetadataVolumesAndNavigationInTheSameOrderWhenChapterNumbersRestart()
    {
        using var context = new Context("""
            [{"id":"20","slug":"v2-1","heading":"Chapter 1","chapter_order":"1","volume_id":"2","volume_name":"Volume 2","volume_order":"2"},
             {"id":"11","slug":"v1-2","heading":"Chapter 2","chapter_order":"2","volume_id":"1","volume_name":"Volume 1","volume_order":"1"},
             {"id":"10","slug":"v1-1","heading":"Chapter 1","chapter_order":"1","volume_id":"1","volume_name":"Volume 1","volume_order":"1"}]
            """);
        context.Chapter(11);
        var info = await context.Source.GetSeriesInfo(SeriesUrl);
        Assert.Equal(SeriesUrl + "v1-1/", info?.FirstChapterUrl);
        var volumes = await context.Source.Volumes(SeriesUrl).ToArrayA();
        Assert.Equal(["Volume 1", "Volume 2"], volumes.Select(v => v.Title));
        Assert.Equal([SeriesUrl + "v1-1/", SeriesUrl + "v1-2/"], volumes[0].Chapters.Select(c => c.Url));
        var chapter = await context.Source.GetChapter(SeriesUrl + "v1-2/", "Volume 1");
        Assert.Equal(SeriesUrl + "v2-1/", chapter?.NextUrl);
    }

    [Fact]
    public async Task LoadsAChapterBySlugAndResumesFromThatChapter()
    {
        using var context = new Context("""
            [{"id":"1","slug":"chapter-1","heading":"Chapter 1","chapter_order":"1"},
             {"id":"2","slug":"chapter-2","heading":"Chapter 2","chapter_order":"2"},
             {"id":"3","slug":"chapter-3","heading":"Chapter 3","chapter_order":"3"}]
            """);
        context.Chapter(2);
        context.Chapter(3);
        var chapters = await context.Source.Chapters(SeriesUrl + "chapter-2/?from=toc#reader").ToArrayA();
        Assert.Equal([SeriesUrl + "chapter-2/", SeriesUrl + "chapter-3/"], chapters.Select(c => c.Url));
        Assert.Equal(SeriesUrl + "chapter-3/", chapters[0].NextUrl);
        Assert.Equal(string.Empty, chapters[1].NextUrl);
        Assert.All(chapters, chapter => Assert.Contains("<em>potion</em>", chapter.Content));
        Assert.Equal(SeriesUrl, context.Source.SeriesFromChapter(chapters[0].Url));
        Assert.DoesNotContain(context.Http.Requests, url => url.Contains("/chapter/1?"));
    }

    [Theory]
    [InlineData(true, "<p>Unlock this chapter.</p>")]
    [InlineData(false, "")]
    [InlineData(false, "   ")]
    public async Task DoesNotImportProtectedOrEmptyChapterContent(bool isProtected, string html)
    {
        using var context = new Context("""[{"id":"1","slug":"chapter-1","heading":"Chapter 1","chapter_order":"1"}]""");
        context.Chapter(1, html, isProtected);
        Assert.Null(await context.Source.GetChapter(SeriesUrl + "chapter-1/", "Volume 1"));
    }

    [Fact]
    public async Task HandlesMissingSeriesAndEmptyCatalogs()
    {
        using var missing = new Context("[]");
        missing.Http.Responses[SeriesApi] = "[]";
        Assert.Null(await missing.Source.GetSeriesInfo(SeriesUrl));
        Assert.Empty(await missing.Source.Volumes(SeriesUrl).ToArrayA());
        using var empty = new Context("[]");
        Assert.Null((await empty.Source.GetSeriesInfo(SeriesUrl))?.FirstChapterUrl);
        Assert.Empty(await empty.Source.Volumes(SeriesUrl).ToArrayA());
    }

    private sealed class Context : IDisposable
    {
        public FixtureHttp Http { get; } = new();
        private readonly ServiceProvider _provider;
        public IDragonholicSourceService Source { get; }
        public IDragonholicApiService Api { get; }

        public Context(string chapters, string unlocked = "[]")
        {
            Http.Responses[SeriesApi] = SeriesJson;
            Http.Responses[CatalogApi] = $$"""{"success":true,"chapters":{{chapters}},"unlockedChapterIds":{{unlocked}},"hasMore":false}""";
            _provider = new ServiceCollection().AddLogging().AddCardboardHttp().AddJson().AddLightNovel()
                .AddSingleton<IHttpClientFactory>(Http).BuildServiceProvider();
            Source = _provider.GetRequiredService<IDragonholicSourceService>();
            Api = _provider.GetRequiredService<IDragonholicApiService>();
        }

        public void Chapter(long id, string html = "<p>A witch brewed a <em>potion</em>.</p>", bool isProtected = false)
        {
            Http.Responses[$"{Root}/wp-json/wp/v2/chapter/{id}?_fields=id,parent,slug,link,title,content"] =
                JsonSerializer.Serialize(new { id, parent = 100, content = new { rendered = html, @protected = isProtected } });
        }

        public void Dispose()
        {
            _provider.Dispose();
            Http.Dispose();
        }
    }

    private sealed class FixtureHttp : HttpMessageHandler, IHttpClientFactory
    {
        public Dictionary<string, string> Responses { get; } = [];
        public List<string> Requests { get; } = [];
        public HttpClient CreateClient(string name) => new(this, false);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requests.Add(url);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Responses[url], Encoding.UTF8, "application/json")
            });
        }
    }
}
