using CardboardBox.Extensions;
using CardboardBox.LightNovel.Core;
using CommandLine;
using System.Runtime.CompilerServices;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("manual-load", HelpText = "Manually catch up from local MD / HTML files")]
public class ManualLoadOptions
{
    public const string DEFAULT_DIRECTORY = "F:\\Books\\to-be-loaded";

    [Option('d', "directory", HelpText = "Directory to load files from", Default = DEFAULT_DIRECTORY)]
    public string? Directory { get; set; } = DEFAULT_DIRECTORY;

    [Option('m', "ignore-md", HelpText = "Ignore Markdown files", Default = false)]
    public bool IgnoreMdFiles { get; set; } = false;

    [Option('h', "ignore-html", HelpText = "Ignore HTML files", Default = false)]
    public bool IgnoreHtmlFiles { get; set; } = false;
}

internal class ManualLoadVerb(
    ILogger<ManualLoadVerb> logger,
    ILnDbService _db,
    INovelApiService _api,
    IMarkdownService _markdown) : BooleanVerb<ManualLoadOptions>(logger)
{
    public static string FixCharacters(string data)
    {
        var items = new Dictionary<string, string>
        {
            { "“", "\"" },
            { "”", "\"" },
            { "’", "'" },
            { "—", "-" },
            { "…", "..." },
        };

        foreach (var item in items)
            data = data.Replace(item.Key, item.Value);

        return data;
    }

    public async Task<string?> GetHtmlFile(string path, string dir, CancellationToken token)
    {
        _logger.LogInformation("Generating HTML from MD file: {path}", path);
        var data = await File.ReadAllTextAsync(path, token);
        if (string.IsNullOrEmpty(data))
        {
            _logger.LogWarning("Empty file: {path}", path);
            return null;
        }

        data = FixCharacters(data);

        var html = _markdown.ToHtml(data);
        if (string.IsNullOrEmpty(html))
        {
            _logger.LogWarning("Failed to convert markdown to HTML: {path}", path);
            return null;
        }

        html = html
            .Replace("&quot;\r\n", "&quot;</p>\r\n<p>")
            .Replace("&quot;\n", "&quot;</p>\n<p>");

        var fileName = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrEmpty(fileName))
        {
            _logger.LogWarning("Invalid file name: {path}", path);
            return null;
        }

        var htmlPath = Path.Combine(dir, $"{fileName}.html");
        await File.WriteAllTextAsync(htmlPath, html, token);
        _logger.LogInformation("HTML file generated: {htmlPath}", htmlPath);
        return htmlPath;
    }

    public async IAsyncEnumerable<(long seriesId, string path, string fileName)> GetFiles(string dir, bool ignoreMd, bool ignoreHtml, [EnumeratorCancellation] CancellationToken token)
    {
        //Ensure the directory exists
        if (!Directory.Exists(dir))
        {
            _logger.LogWarning("Directory does not exist: {dir}", dir);
            yield break;
        }

        //Get all of the sub directories
        var directories = Directory.GetDirectories(dir);
        if (directories is null || directories.Length == 0)
        {
            _logger.LogWarning("No directories found: {dir}", dir);
            yield break;
        }

        //Iterate through each directory
        foreach (var directory in directories)
        {
            //Get the last part of the directory name - this should be the series ID
            var last = directory.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            if (last is null || !long.TryParse(last, out var seriesId))
            {
                _logger.LogWarning("Invalid directory name: {last} >> {dir}", last, dir);
                continue;
            }

            //Ensure we dedupe the files to be loaded
            var processed = new HashSet<string>();
            var files = Directory.GetFiles(directory).Order().ToArray();
            if (files is null || files.Length == 0)
            {
                _logger.LogWarning("No files found in directory: {dir}", directory);
                continue;
            }

            //Iterate through each file
            foreach (var file in files)
            {
                //Get the name and extension
                var fileName = Path.GetFileNameWithoutExtension(file);
                var extension = Path.GetExtension(file).Trim('.').ToLower().Trim();
                if (string.IsNullOrEmpty(fileName))
                {
                    _logger.LogWarning("Invalid file name: {file}", file);
                    continue;
                }

                if (string.IsNullOrEmpty(extension))
                {
                    _logger.LogWarning("Invalid file extension: {file}", file);
                    continue;
                }

                //Ensure we haven't already processed the file
                if (processed.Contains(fileName)) continue;
                //HTML files will be processed first due to the order
                if (extension == "html")
                {
                    if (ignoreHtml) continue;

                    processed.Add(fileName);
                    yield return (seriesId, file, fileName);
                    continue;
                }
                //Ignore anything that isn't an MD file
                if (extension != "md")
                {
                    _logger.LogDebug("Invalid file extension: {file}", file);
                    continue;
                }
                //Ignore the MD file if we're ignoring them
                if (ignoreMd)
                {
                    _logger.LogInformation("Ignoring markdown file: {file}", file);
                    continue;
                }
                //Get the HTML file for the MD file
                var html = await GetHtmlFile(file, directory, token);
                if (html is null)
                {
                    _logger.LogWarning("Unable to read file: {file}", file);
                    continue;
                }
                //Add it to the processed list
                processed.Add(fileName);
                yield return (seriesId, html, fileName);
            }
        }
    }

    public static string BuildPageUrl(Page page, string title)
    {
        var endsWithSlash = page.Url.EndsWith('/');
        var url = page.Url.Trim('/');
        var parts = url.Split('/');
        parts[^1] = string.Join("-", title.PurgePathChars().Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)).Replace("'", "");
        return string.Join('/', parts).ToLower() + (endsWithSlash ? "/" : "");
    }

    public override async Task<bool> Execute(ManualLoadOptions options, CancellationToken token)
    {
        var dir = options.Directory ?? ManualLoadOptions.DEFAULT_DIRECTORY;
        if (!Directory.Exists(dir))
        {
            _logger.LogWarning("Directory {dir} does not exist", dir);
            return false;
        }

        var files = GetFiles(dir, options.IgnoreMdFiles, options.IgnoreHtmlFiles, token)
            .GroupBy(x => x.seriesId);
        await foreach (var series in files)
        {
            var seriesId = series.Key;
            var seriesInfo = await _db.Series.Fetch(seriesId);
            if (seriesInfo is null)
            {
                _logger.LogWarning("Series with ID {seriesId} not found", seriesId);
                continue;
            }

            _logger.LogInformation("Processing series {seriesId} - {seriesTitle}", seriesId, seriesInfo.Title);
            var books = await _db.Books.BySeries(seriesId);
            if (books is null || books.Length == 0)
            {
                _logger.LogWarning("No books found for series {seriesId}", seriesId);
                continue;
            }

            var book = books.OrderByDescending(x => x.Ordinal).First();
            _logger.LogInformation("Found last book for series {seriesId} - {bookId} - {bookTitle}", seriesId, book.Id, book.Title);

            var chapters = await _db.Chapters.ByBook(book.Id);
            if (chapters is null || chapters.Length == 0)
            {
                _logger.LogWarning("No chapters found for book {bookId} - {bookTitle}", book.Id, book.Title);
                continue;
            }

            var lastChapter = chapters.OrderByDescending(x => x.Ordinal).First();
            var chapterOrdinal = lastChapter.Ordinal;

            var lastPage = await _db.Pages.LastPage(seriesId);
            if (lastPage is null)
            {
                _logger.LogWarning("No last lastPage found for series {seriesId}", seriesId);
                continue;
            }

            var pageOrdinal = lastPage.Ordinal;

            foreach(var file in series.OrderBy(t => t.fileName))
            {
                _logger.LogInformation("Processing file {file} for series {seriesId} into book {booKId} >> {path}", 
                    file.fileName, seriesInfo.Id, book.Id, file.path);

                var data = await File.ReadAllTextAsync(file.path, token);
                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogWarning("Empty file: {file}", file.path);
                    continue;
                }
                var url = BuildPageUrl(lastPage, file.fileName);
                var hash = url.MD5Hash();

                var pageExists = await _db.Pages.ByHashId(hash);
                if (pageExists != null)
                {
                    _logger.LogWarning("Page already exists: {url}", url);
                    continue;
                }

                chapterOrdinal++;
                pageOrdinal++;
                var page = new Page
                {
                    SeriesId = seriesId,
                    Ordinal = pageOrdinal,
                    HashId = hash,
                    Title = file.fileName,
                    Url = url,
                    NextUrl = null,
                    Content = data,
                    Mimetype = "application/html"
                };
                page.Id = await _db.Pages.Insert(page);

                var chapter = _api.ChapterFromPage(page, book, chapterOrdinal);
                chapter.Id = await _db.Chapters.Insert(chapter);

                var chapterPage = new ChapterPage
                {
                    ChapterId = chapter.Id,
                    PageId = page.Id,
                    Ordinal = 0
                };
                chapterPage.Id = await _db.ChapterPages.Insert(chapterPage);
                _logger.LogInformation("Added chapter [C::{chapterId} || P::{pageId} || CP:{chapterPageId}] - {chapterTitle} to book {bookId} - {bookTitle}",
                    chapter.Id, page.Id, chapterPage.Id, chapter.Title, book.Id, book.Title);
            }
        }
         
        return true;
    }
}
