using CardboardBox.LightNovel.Core.Sources.Utilities;
using CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;
using CommandLine;
using System.Security.Cryptography;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("html-style-test", HelpText = "Runs the HTML style cleanup utility against URL, file, inline HTML, or generated sample HTML")]
public class HtmlStyleTestOptions
{
    [Option('u', "url", HelpText = "The URL to fetch with FlareSolverr")]
    public string? Url { get; set; }

    [Option('f', "file", HelpText = "The local HTML file path to read")]
    public string? FilePath { get; set; }

    [Option("html", HelpText = "The raw HTML content to clean")]
    public string? Html { get; set; }

    [Option('o', "output", HelpText = "The output file path for the cleaned HTML", Default = "cleaned-html.html")]
    public string Output { get; set; } = "cleaned-html.html";
}

internal class HtmlStyleTestVerb(
    IFlareSolver _flare,
    ILogger<HtmlStyleTestVerb> logger) : BooleanVerb<HtmlStyleTestOptions>(logger)
{
    private const string CacheDirectory = "html-cache";

    public override async Task<bool> Execute(HtmlStyleTestOptions options, CancellationToken token)
    {
        var specifiedInputs = new[]
        {
            options.Url,
            options.FilePath,
            options.Html
        }.Count(t => !string.IsNullOrWhiteSpace(t));

        if (specifiedInputs > 1)
        {
            _logger.LogError("Specify only one input source: --url, --file, or --html.");
            return false;
        }

        var html = await ResolveHtml(options);

        var doc = new HtmlDocument
        {
            OptionFixNestedTags = true,
            OptionAutoCloseOnEnd = true,
            OptionWriteEmptyNodes = true,
        };
        doc.LoadHtml(html);

        var inlined = HtmlStyleUtility.InlineDocumentStyles(doc);
        var removed = HtmlStyleUtility.RemoveHiddenBodyElements(doc);

        var output = Path.GetFullPath(options.Output);
        var outputDirectory = Path.GetDirectoryName(output);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        await File.WriteAllTextAsync(output, doc.DocumentNode.OuterHtml, token);

        _logger.LogInformation("Wrote cleaned HTML to {output}. Inlined {inlined} style applications and removed {removed} hidden elements.",
            output, inlined, removed);
        return true;
    }

    private async Task<string> ResolveHtml(HtmlStyleTestOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Url))
            return await ResolveUrlHtml(options.Url);

        if (!string.IsNullOrWhiteSpace(options.FilePath))
        {
            var path = Path.GetFullPath(options.FilePath);
            _logger.LogInformation("Reading HTML from {path}", path);
            return await File.ReadAllTextAsync(path);
        }

        if (!string.IsNullOrWhiteSpace(options.Html))
        {
            _logger.LogInformation("Reading HTML from inline --html option");
            return options.Html;
        }

        _logger.LogInformation("No input specified; using generated sample HTML");
        return GeneratedHtml();
    }

    private async Task<string> ResolveUrlHtml(string url)
    {
        Directory.CreateDirectory(CacheDirectory);

        var cachePath = Path.Combine(CacheDirectory, $"{HashUrl(url)}.html");
        if (File.Exists(cachePath))
        {
            _logger.LogInformation("Reading cached HTML for {url} from {cachePath}", url, cachePath);
            return await File.ReadAllTextAsync(cachePath);
        }

        _logger.LogInformation("Fetching {url} via FlareSolverr", url);
        var response = await _flare.Get(url, timeout: 30_000);
        if (response?.Solution is null)
            throw new InvalidOperationException($"FlareSolverr did not return a solution for {url}");

        if (response.Solution.Status < 200 || response.Solution.Status >= 300)
            throw new InvalidOperationException($"FlareSolverr returned HTTP {response.Solution.Status} for {url}");

        var html = response.Solution.Response;
        await File.WriteAllTextAsync(cachePath, html);
        _logger.LogInformation("Cached HTML for {url} to {cachePath}", url, cachePath);
        return html;
    }

    private static string HashUrl(string url)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GeneratedHtml()
    {
        return """
            <!doctype html>
            <html>
            <head>
                <style>
                    .hidden { display: none; }
                    #chapter .ad, article > p.spoiler, [data-hidden="true"] { display: none !important; }
                    .visible { color: #123456; }
                </style>
            </head>
            <body>
                <article id="chapter">
                    <p class="visible">This paragraph should remain and receive an inline color style.</p>
                    <p class="hidden">This class-hidden paragraph should be removed.</p>
                    <div class="ad">This descendant selector match should be removed.</div>
                    <p class="spoiler">This child selector match should be removed.</p>
                    <span data-hidden="true">This attribute selector match should be removed.</span>
                    <p style="display: none;">This inline-hidden paragraph should be removed.</p>
                </article>
            </body>
            </html>
            """;
    }
}
