using System.Net;
using System.Text;
using CardboardBox.Http;
using CardboardBox.Json;
using CardboardBox.LightNovel.Core.Sources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CardboardBox.LightNovel.Core.Tests;

public class LntSourceTests
{
    private const string Series = "https://lightnovelstranslations.com/novel/example/";
    private const string Images = Series + "?tab=novel_illustrations";
    private const string Volume2 = Series + "?tab=table_contents&lnt_volume=185";
    private const string Page2 = Volume2 + "&lnt_page=2";

    [Fact]
    public async Task LoadsLazyVolumesAllChapterPagesAndSeparateIllustrationGalleries()
    {
        using var context = new Context();
        context.Http.Responses[Series] = Catalog("""
            <div class="lnt-chapter-page" data-page="1">
              <ul><li class="chapter-item unlock"><span>1:</span><a href="first/">Chapter &amp; One</a></li></ul>
            </div>
            """, "");
        context.Http.Responses[Volume2] = Catalog("", $$"""
            <div class="lnt-chapter-page" data-page="1">
              <ul><li class="chapter-item unlock"><span>1:</span><a href="second/">Chapter Two</a></li>
                  <li class="chapter-item lock"><a href="locked/">Locked</a></li></ul>
              <nav><a class="lnt-page-link" href="{{Page2.Replace("&", "&#038;")}}">Next chapters</a></nav>
            </div>
            """);
        context.Http.Responses[Page2] = Catalog("", $$"""
            <div class="lnt-chapter-page" data-page="2">
              <ul><li class="chapter-item unlock"><span>1:</span><a href="second/">Chapter Two</a></li>
                  <li class="chapter-item unlock"><span>2:</span><a href="https://lightnovelstranslations.com/novel/example/third-%e3%80%8c/">Chapter Three</a></li>
                  <li class="chapter-item lock unlock"><a href="locked/">Locked</a></li></ul>
              <nav><a class="lnt-page-link" href="{{Volume2}}">Previous chapters</a></nav>
            </div>
            """);
        context.Http.Responses[Images] = $$"""
            <nav class="lnt-gallery-nav"><a href="{{Images}}&amp;lnt_gallery=1">Volume 1</a>
              <a href="{{Images}}&amp;lnt_gallery=2">Volume 2</a></nav>
            <section class="lnt-gallery-content"><h3>Volume 1</h3><p><img src="/cover1.webp"></p></section>
            """;
        context.Http.Responses[Images + "&lnt_gallery=2"] = """
            <section class="lnt-gallery-content"><h3>Volume 2</h3><p><picture><img src="/cover2.webp"></picture></p></section>
            """;

        var volumes = await context.Source.Volumes(Series).ToArrayA();

        Assert.Equal(["Volume 1", "Volume 2"], volumes.Select(v => v.Title));
        Assert.Equal([Series + "first/"], volumes[0].Chapters.Select(c => c.Url));
        Assert.Equal("1: Chapter & One", volumes[0].Chapters[0].Title);
        Assert.Equal([Series + "second/", Series + "third-%e3%80%8c/"], volumes[1].Chapters.Select(c => c.Url));
        Assert.Equal(["https://lightnovelstranslations.com/cover1.webp"], volumes[0].Forwards);
        Assert.Equal(["https://lightnovelstranslations.com/cover2.webp"], volumes[1].Forwards);
        Assert.All(volumes, v => Assert.Equal(Series, v.Url));
        Assert.Equal([Series, Images, Volume2, Page2, Images + "&lnt_gallery=2"], context.Http.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SupportsLegacyCatalogsWithOrWithoutIllustrations(bool includeImages)
    {
        using var context = new Context();
        context.Http.Responses[Series] = """
            <div class="novel_list_chapter_content"><div class="accordition_item">
              <h3 class="accordition_item_title">Volume 1</h3><div class="accordition_item_content"><ul>
                <li class="unlock"><span>1:</span><a href="first/">Chapter 1</a></li>
                <li class="lock"><span>2:</span><a href="locked/">Chapter 2</a></li>
              </ul></div></div></div>
            """ + (includeImages ? """
            <div class="novel_list_chapter_content"><div class="accordition_item">
              <h3 class="accordition_item_title">Volume 1</h3>
              <div class="accordition_item_content"><p><img src="/cover.webp"></p></div>
            </div></div>
            """ : "");

        var volume = Assert.Single(await context.Source.Volumes(Series).ToArrayA());
        Assert.Equal("Volume 1", volume.Title);
        Assert.Equal(Series + "first/", Assert.Single(volume.Chapters).Url);
        Assert.Equal(includeImages ? ["https://lightnovelstranslations.com/cover.webp"] : [], volume.Forwards);
        Assert.Equal([Series], context.Http.Requests);
    }

    [Fact]
    public async Task ReadsTheNewStartReadingLinkAndNormalizesSeriesQueries()
    {
        using var context = new Context();
        context.Http.Responses[Series] = $$"""
            <div class="novel_title"><h3>Example Novel</h3></div>
            <div class="novel_tags_item"><span>Science Fiction</span></div>
            <div class="novel_detail_info"><ul><li>Author: Example Author</li></ul></div>
            <div class="col-2 novel-image"><img src="https://lightnovelstranslations.com/cover.webp"></div>
            <a id="lnt-reading-button" href="{{Series}}first/">Start Reading</a>
            """;
        var info = await context.Source.GetSeriesInfo(Series.TrimEnd('/') + "?tab=table_contents");
        Assert.NotNull(info);
        Assert.Equal(Series + "first/", info.FirstChapterUrl);
        Assert.Equal(["Example Author"], info.Authors);
        Assert.Equal("https://lightnovelstranslations.com/cover.webp", info.Image);
        Assert.Equal(Series, context.Source.SeriesFromChapter(Series + "first/?from=toc#reader"));
    }

    [Fact]
    public async Task LoadsChapterContentAndNavigationWithAdditionalCssClasses()
    {
        using var context = new Context();
        context.Http.Responses[Series + "first/"] = """
            <div class="next_story_btn enabled"><a href="../second/">Next Page</a></div>
            <div class="text_story reader"><h2>Chapter &amp; One</h2><p>&nbsp;</p><hr>
              <p>A <em>spaceship</em> arrived.</p><p><img src="/illustration.webp" class="lazy"></p>
              <div class="lnt-ad">Advertisement</div><script>advertisement()</script><hr>
            </div>
            """;
        var chapter = await context.Source.GetChapter(Series + "first/", "Volume 1");
        Assert.NotNull(chapter);
        Assert.Equal("Chapter & One", chapter.ChapterTitle);
        Assert.Equal("Volume 1", chapter.BookTitle);
        Assert.Equal(Series + "second/", chapter.NextUrl);
        Assert.Contains("<em>spaceship</em>", chapter.Content);
        Assert.Contains("illustration.webp", chapter.Content);
        Assert.DoesNotContain("Advertisement", chapter.Content);
        Assert.DoesNotContain("<hr", chapter.Content);
        Assert.DoesNotContain("<script", chapter.Content);
    }

    [Theory]
    [InlineData("<h2>Locked</h2><div class='excerpt-text'><div class='unlock_chapter text-center new-class'>Unlock</div></div>")]
    [InlineData("<h2>Empty</h2><p>&nbsp;</p><hr>")]
    [InlineData("<h2>Empty</h2>")]
    public async Task DoesNotImportLockedOrEmptyChapters(string html)
    {
        using var context = new Context();
        context.Http.Responses[Series + "first/"] = $"<div class='text_story'>{html}</div>";
        Assert.Null(await context.Source.GetChapter(Series + "first/", "Volume 1"));
    }

    private static string Catalog(string volume1, string volume2) => $$"""
        <div class="novel_list_chapter_content">
          <section class="lnt-volume" data-volume="132"><h3><a class="lnt-volume-link" href="{{Series}}?tab=table_contents&amp;lnt_volume=132">Volume 1<span>1 chapter</span></a></h3>
            <div class="lnt-volume-panel">{{volume1}}</div></section>
          <section class="lnt-volume" data-volume="185"><h3><a class="lnt-volume-link" href="{{Volume2}}">Volume 2<span>3 chapters</span></a></h3>
            <div class="lnt-volume-panel">{{volume2}}</div></section>
        </div>
        """;

    private sealed class Context : IDisposable
    {
        public FixtureHttp Http { get; } = new();
        private readonly ServiceProvider _provider;
        public ILntSourceService Source { get; }

        public Context()
        {
            _provider = new ServiceCollection().AddLogging().AddCardboardHttp().AddJson().AddLightNovel()
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddSingleton<IHttpClientFactory>(Http).BuildServiceProvider();
            Source = _provider.GetRequiredService<ILntSourceService>();
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
                Content = new StringContent(Responses[url], Encoding.UTF8, "text/html")
            });
        }
    }
}
