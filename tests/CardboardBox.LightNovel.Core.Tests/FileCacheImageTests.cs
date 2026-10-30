using System.Net;
using CardboardBox.Anime.Core;
using CardboardBox.Http;
using CardboardBox.Json;
using ImageMagick;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CardboardBox.LightNovel.Core.Tests;

public class FileCacheImageTests
{
    private const string Origin = "https://lightnovelstranslations.com/wp-content/uploads/2019/12/Strongest-Starship-Volume-1-0-693x1024-1.webp";

    [Theory]
    [InlineData("i0.wp.com")]
    [InlineData("i1.wp.com")]
    [InlineData("i2.wp.com")]
    public async Task DownloadsOriginalAndConvertsItForEpubWithoutContactingWordPressProxy(string host)
    {
        using var image = new MagickImage(MagickColors.SkyBlue, 2, 2);
        using var http = new OriginalImageHttp(image.ToByteArray(MagickFormat.WebP));
        using var services = new ServiceCollection().AddLogging().AddCardboardHttp().AddJson()
            .AddSingleton<IHttpClientFactory>(http).BuildServiceProvider();
        var cacheDirectory = Path.Combine(Path.GetTempPath(), "lnt-image-cache-" + Guid.NewGuid().ToString("N"));
        try
        {
            var cache = new FileCacheService(services.GetRequiredService<IApiService>(), cacheDirectory);
            var epub = new NovelEpubService(null!, NullLogger<NovelEpubService>.Instance, cache);
            var proxyUrl = $"https://{host}/lightnovelstranslations.com/wp-content/uploads/2019/12/Strongest-Starship-Volume-1-0-693x1024-1.webp?resize=600%2C887&ssl=1";

            // Concurrent volumes may refer to the same upload using different
            // proxy sizes or its original URL. They must share a single download.
            var results = await Task.WhenAll(epub.GetData(proxyUrl), epub.GetData(Origin),
                epub.GetData(proxyUrl.Replace("600%2C887", "400%2C")));
            foreach (var result in results)
            {
                using var stream = result.Stream;
                Assert.Equal("image/png", result.Mimetype);
                using var decoded = new MagickImage(stream);
                Assert.Equal(MagickFormat.Png, decoded.Format);
            }
            var cached = await epub.GetData(proxyUrl);
            using (cached.Stream)
                Assert.True(cached.Stream.Length > 0);
            Assert.Equal([Origin], http.Requests);
        }
        finally
        {
            // This is a unique test directory; delete only its cache files.
            if (Directory.Exists(cacheDirectory))
            {
                foreach (var file in Directory.GetFiles(cacheDirectory)) File.Delete(file);
                Directory.Delete(cacheDirectory);
            }
        }
    }

    [Theory]
    [InlineData(Origin)]
    [InlineData("https://i0.wp.com/another-site.example/image.webp?resize=600%2C")]
    [InlineData("https://i0.wp.com/lightnovelstranslations.com/not-an-upload.webp?resize=600%2C")]
    [InlineData("https://i0.wp.com.example/lightnovelstranslations.com/wp-content/uploads/image.webp")]
    [InlineData("file://C:/image.webp")]
    [InlineData("data:image/png;base64,AA==")]
    public void LeavesOtherUrlsUnchanged(string url) => Assert.Equal(url, FileCacheService.NormalizeUrl(url));

    private sealed class OriginalImageHttp(byte[] bytes) : HttpMessageHandler, IHttpClientFactory
    {
        public List<string> Requests { get; } = [];
        public HttpClient CreateClient(string name) => new(this, false);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Requests.Add(url);
            await Task.Yield();
            // Deliberately fail any proxy request. Only the original is available.
            var response = new HttpResponseMessage(url == Origin ? HttpStatusCode.OK : HttpStatusCode.NotFound)
            {
                Content = new ByteArrayContent(bytes)
            };
            response.Content.Headers.ContentType = new("image/webp");
            return response;
        }
    }
}
