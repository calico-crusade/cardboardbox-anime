using System.IO.Compression;
using System.Xml.Linq;
using CardboardBox.Epub;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CardboardBox.LightNovel.Core.Tests;

public class EpubEntityTests
{
    private static readonly XNamespace Xhtml = "http://www.w3.org/1999/xhtml";
    private const string Text = "one&#8230; two&#8127; three&#x2026; four&#8217;";
    private const string DisplayedText = "one… two᾿ three… four’";

    [Fact]
    public void CountsDecodedWordsAndNeverSplitsAnEntityAtAPageBoundary()
    {
        var generator = new EpubPageBreakGenerator(2);
        var output = generator.AddToXhtmlPage(Html($"<p>{Text}</p>"), "chapter.xhtml");
        var doc = XDocument.Parse(output);
        var paragraph = doc.Descendants(Xhtml + "p").Single();

        Assert.Equal(DisplayedText, paragraph.Value);
        Assert.Equal(2, generator.PageBreaks.Count);
        Assert.DoesNotContain("&amp;#", output);
    }

    [Fact]
    public void PreservesNamedEntitiesSupplementaryCharactersAndEscapedMarkupAsText()
    {
        var output = new EpubPageBreakGenerator(2).AddToXhtmlPage(Html("""
            <p>Fish &amp; chips &ldquo;quoted&rdquo; &#x1F680; &nbsp; &lt;em&gt;text&lt;/em&gt; &amp;#8230; &amp;lt;em&amp;gt; &amp;amp;</p>
            """), "chapter.xhtml");
        var paragraph = XDocument.Parse(output).Descendants(Xhtml + "p").Single();

        Assert.Equal("Fish & chips “quoted” 🚀 \u00a0 <em>text</em> &#8230; &lt;em&gt; &amp;", paragraph.Value);
        Assert.Empty(paragraph.Descendants(Xhtml + "em"));
    }

    [Fact]
    public void PreservesAttributeValuesAndTextWhenPageBreaksAreRegenerated()
    {
        var html = Html("""
            <p><a href="https://example.test/?one=1&amp;two=2" title="Wait&#8230; &quot;quoted&quot; &amp;amp;">Wait&#8230;</a></p>
            """);
        for (var i = 0; i < 2; i++)
        {
            html = new EpubPageBreakGenerator(2).AddToXhtmlPage(html, "chapter.xhtml");
            var link = XDocument.Parse(html).Descendants(Xhtml + "a").Single();
            Assert.Equal("https://example.test/?one=1&two=2", (string?)link.Attribute("href"));
            Assert.Equal("Wait… \"quoted\" &amp;", (string?)link.Attribute("title"));
            Assert.Equal("Wait…", link.Value);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PackagesReadableChapterTextAndKeepsItReadableAfterAddingPageBreaksAgain(bool withImage)
    {
        var path = Path.GetTempFileName();
        var repaginated = Path.GetTempFileName();
        try
        {
            var service = new NovelEpubService(null!, NullLogger<NovelEpubService>.Instance, null!);
            var image = withImage ? "<img src='data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=' alt='&#8127;'>" : "";
            await using (var epub = EpubBuilder.Create("Volume 12", path))
            {
                var builder = await epub.Start();
                builder.EnableApproximatePageBreaks(2);
                await service.HandleChapters(builder, new Book(), [new ChapterScaffold
                {
                    Chapter = new() { Title = "Chapter 598…" },
                    Pages = [new() { Page = new() { Mimetype = "application/html", Content = $"<p>{Text}</p>{image}" } }]
                }]);
            }
            await CheckChapter(path, withImage);

            EpubPageBreakWriter.AddApproximatePageBreaks(path, repaginated, 2);
            await CheckChapter(repaginated, withImage);
            using var archive = ZipFile.OpenRead(repaginated);
            using var navStream = archive.GetEntry("OEPBS/Text/toc.xhtml")!.Open();
            var nav = XDocument.Load(navStream);
            Assert.Contains(nav.Descendants(Xhtml + "a"), a => a.Value == "Chapter 598…");
        }
        finally
        {
            File.Delete(path);
            File.Delete(repaginated);
        }
    }

    private static async Task CheckChapter(string path, bool withImage)
    {
        using var archive = ZipFile.OpenRead(path);
        using var reader = new StreamReader(archive.GetEntry("OEPBS/Text/chapter-0-0.xhtml")!.Open());
        var content = await reader.ReadToEndAsync();
        var doc = XDocument.Parse(content);
        Assert.Equal(DisplayedText, doc.Descendants(Xhtml + "p").Single().Value);
        Assert.DoesNotContain("&amp;#", content);
        if (withImage)
        {
            var image = doc.Descendants(Xhtml + "img").Single();
            Assert.Equal("᾿", (string?)image.Attribute("alt"));
            Assert.NotNull(archive.GetEntry("OEPBS/" + ((string)image.Attribute("src")!)[3..]));
        }
    }

    private static string Html(string body) => $"<html xmlns='{Xhtml}' xmlns:epub='http://www.idpf.org/2007/ops'><head><title>Entities</title></head><body>{body}</body></html>";
}
