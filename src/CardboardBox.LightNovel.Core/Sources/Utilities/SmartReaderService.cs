using SmartReader;

namespace CardboardBox.LightNovel.Core.Sources.Utilities;

using Article = (string? title, string? content);

/// <summary>
/// Service for reading and cleansing HTML content
/// </summary>
public interface ISmartReaderService
{
    /// <summary>
    /// Ensures the url is absolute
    /// </summary>
    /// <param name="url">The URL to check</param>
    /// <param name="root">The root url to use</param>
    /// <returns>The absolute URL</returns>
    string FixUrl(string url, string root);

    /// <summary>
    /// Cleans up markdown image references to be HTML
    /// </summary>
    /// <param name="markdown">The markdown in question</param>
    /// <param name="root">The root URL to absolute any images</param>
    /// <returns>The cleaned up HTML</returns>
    string FixImages(string markdown, string root);

    /// <summary>
    /// Flattens the given HTML to get just the content
    /// </summary>
    /// <param name="html">The HTML to clean</param>
    /// <param name="root">The root URL to use to absolute images</param>
    /// <param name="onFlatten">A function to run whenever a node is identified</param>
    /// <returns>The cleaned HTML</returns>
    string CleanseHtml(string html, string root, Func<HtmlNode, HtmlNode>? onFlatten = null);

    /// <summary>
    /// Gets the article content from the given document
    /// </summary>
    /// <param name="html">The HTML to use</param>
    /// <param name="url">The URL of the page</param>
    /// <returns>The title and content of the article</returns>
    Task<Article> GetArticle(string html, string url);

    /// <summary>
    /// Gets the article content from the given document
    /// </summary>
    /// <param name="doc">The HTML to use</param>
    /// <param name="url">The URL of the page</param>
    /// <returns>The title and content of the article</returns>
    Task<Article> GetArticle(HtmlDocument doc, string url);

    /// <summary>
    /// Gets the cleaned article content from the given document
    /// </summary>
    /// <param name="html">The HTML to use</param>
    /// <param name="url">The URL of the page</param>
    /// <returns>The title and content of the article</returns>
    Task<Article> GetCleanArticle(string html, string url);

    /// <summary>
    /// Gets the cleaned article content from the given document
    /// </summary>
    /// <param name="doc">The HTML to use</param>
    /// <param name="url">The URL of the page</param>
    /// <returns>The title and content of the article</returns>
    Task<Article> GetCleanArticle(HtmlDocument doc, string url);
}

internal class SmartReaderService(
    IPurgeUtils _purge,
    IMarkdownService _markdown,
    ILogger<SmartReaderService> _logger) : ISmartReaderService
{
    public string FixUrl(string url, string root)
    {
        if (url.StartsWith("//"))
            url = "https:" + url;
        if (url.StartsWith("http"))
            return url;

        return $"{root.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    public string FixImages(string markdown, string root)
    {
        var mdImg = new Regex(@"!\[(.*?)\]\((.*?)\)");
        var matches = mdImg.Matches(markdown);
        foreach (Match match in matches)
        {
            var imgAlt = match.Groups[1].Value;
            var imgUrl = FixUrl(match.Groups[2].Value, root);
            markdown = markdown.Replace(
                match.Groups[0].Value,
                $"<img alt=\"{imgAlt}\" src=\"{imgUrl}\" />");
        }

        return markdown;
    }

    public string CleanseHtml(string html, string root, Func<HtmlNode, HtmlNode>? onFlatten = null)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var output = new StringBuilder();

        foreach (var node in _purge.Flatten(doc))
        {
            var result = onFlatten?.Invoke(node) ?? node;
            output.AppendLine(result.OuterHtml);
            output.AppendLine();
        }

        var clean = output.ToString();
        var markdown = _markdown.ToMarkdown(clean);
        clean = _markdown.ToHtml(markdown);
        clean = FixImages(clean, root);
        clean = _purge.PurgeBadElements(clean);
        return clean;
    }

    public async Task<Article> GetArticle(string html, string url)
    {
        var reader = new Reader(url, html)
        {
            Debug = true,
            LoggerDelegate = (msg) => _logger.LogTrace("[SMART READER] {url} >> {msg}", url, msg)
        };

        var article = await reader.GetArticleAsync();
        if (article is null || !article.Completed || !article.IsReadable)
        {
            var errors = article?.Errors?.ToArray() ?? [];
            foreach (var error in errors)
                _logger.LogError(error, "[SMART READER] Failed to read >> {url}", url);
            _logger.LogWarning("Could not get article for {url}", url);
            return (null, null);
        }

        return (article.Title, article.Content);
    }

    public Task<Article> GetArticle(HtmlDocument doc, string url)
    {
        return GetArticle(doc.Text, url);
    }

    public async Task<Article> GetCleanArticle(string html, string url)
    {
        var (title, content) = await GetArticle(html, url);
        if (title is null || content is null)
            return (null, null);

        var clean = CleanseHtml(content, url);
        return (title, clean);
    }

    public Task<Article> GetCleanArticle(HtmlDocument doc, string url)
    {
        return GetCleanArticle(doc.Text, url);
    }
}
