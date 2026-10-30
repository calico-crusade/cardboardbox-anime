using System.IO.Compression;
using System.Xml.Linq;
using CardboardBox.Anime.Core;
using CardboardBox.Epub;
using ImageMagick;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CardboardBox.LightNovel.Core.Tests;

public class NovelEpubImageTests
{
    private const string ProxyUrl = "https://i0.wp.com/lightnovelstranslations.com/wp-content/uploads/2019/12/Strongest-Starship-Volume-1-0-693x1024-1.webp?resize=600%2C";

    [Theory]
    [InlineData("cover.webp", "image/jpeg", ".jpg")]
    [InlineData("cover.jpg", "image/png", ".png")]
    [InlineData("cover.png", "image/webp", ".webp")]
    [InlineData("cover.webp", "IMAGE/JPEG; charset=binary", ".jpg")]
    [InlineData("", "image/jpeg", ".jpg")]
    [InlineData("animation.webp", "image/gif", ".gif")]
    [InlineData("drawing.webp", "image/svg+xml", ".svg")]
    public void UsesTheResponseImageTypeInsteadOfTheUrlOrCachedFilename(string name, string mime, string extension)
    {
        var service = new NovelEpubService(null!, NullLogger<NovelEpubService>.Instance, null!);
        var filename = service.DetermineName(ProxyUrl, name, mime);
        Assert.Equal(extension, Path.GetExtension(filename));
        Assert.DoesNotContain('?', filename);
        Assert.DoesNotContain('%', filename);
    }

    [Fact]
    public async Task PackagesProxiedAndCachedJpegsAsJpegsForCoverIllustrationsAndInlineImages()
    {
        using var image = new MagickImage(MagickColors.SkyBlue, 2, 2);
        var bytes = image.ToByteArray(MagickFormat.Jpeg);
        var cache = new CachedImage(bytes);
        var service = new NovelEpubService(null!, NullLogger<NovelEpubService>.Instance, cache);
        var path = Path.GetTempFileName();
        try
        {
            await using (var epub = EpubBuilder.Create("Space Mercenary", path))
            {
                var builder = await epub.Start();
                await service.HandleCoverImage(builder, new Book { CoverImage = ProxyUrl });
                await service.HandleForwards(builder, new Book { Forwards = [ProxyUrl] });
                await builder.AddChapter("Chapter 1", chapter => service.PostFixImages(builder, chapter,
                    "chapter.xhtml", $"<p>A spaceship.</p><img src=\"{ProxyUrl}\">"));
            }

            using var archive = ZipFile.OpenRead(path);
            using var manifestStream = archive.GetEntry("OEPBS/content.opf")!.Open();
            var manifest = XDocument.Load(manifestStream);
            var item = Assert.Single(manifest.Descendants().Where(n =>
                n.Name.LocalName == "item" && ((string?)n.Attribute("media-type"))?.StartsWith("image/") == true));
            Assert.Equal("image/jpeg", (string?)item.Attribute("media-type"));
            var href = (string)item.Attribute("href")!;
            Assert.EndsWith(".jpg", href);
            var entry = archive.GetEntry("OEPBS/" + href)!;
            using var imageStream = entry.Open();
            using var decoded = new MagickImage(imageStream);
            Assert.Equal(MagickFormat.Jpeg, decoded.Format);

            int references = 0;
            foreach (var page in archive.Entries.Where(e => e.FullName.EndsWith(".xhtml")))
            {
                using var reader = new StreamReader(page.Open());
                var content = await reader.ReadToEndAsync();
                if (content.Contains("../" + href)) references++;
                Assert.DoesNotContain(".webp", content);
            }
            Assert.Equal(3, references);
            Assert.Equal([ProxyUrl, ProxyUrl, ProxyUrl], cache.Requests);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class CachedImage(byte[] data) : IFileCacheService
    {
        public List<string> Requests { get; } = [];

        public Task<StreamResult> GetFile(string url)
        {
            Requests.Add(url);
            return Task.FromResult(new StreamResult(new MemoryStream(data), "Strongest-Starship.webp", "image/jpeg"));
        }
    }
}
