using CardboardBox.LightNovel.Core;
using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("novel-epub", HelpText = "Generates an EPUB file from a light novel series")]
public class NovelEPubOptions
{
    [Option('s', "series-id", HelpText = "The ID of the light novel series to generate an EPUB for")]
    public long? SeriesId { get; set; }

    [Option('b', "book-id", HelpText = "The ID of the light novel book to generate an EPUB for")]
    public long? BookId { get; set; }

    [Option('o', "output", HelpText = "The output directory path for the generated EPUB")]
    public string? Output { get; set; }
}

internal class NovelEPubVerb(ILogger<NovelEPubVerb> logger, ILnDbService _db, INovelEpubService _epub) : BooleanVerb<NovelEPubOptions>(logger)
{
    public async Task<long[]> GetBooks(long? seriesId, long? bookId)
    {
        if (bookId is not null) return [ bookId.Value ];

        var series = await _db.Books.BySeries(seriesId ?? 0);
        return series.Select(x => x.Id).ToArray();
    }

    public override async Task<bool> Execute(NovelEPubOptions options, CancellationToken token)
    {
        var seriesId = options.SeriesId;
        var bookId = options.BookId;
        var output = options.Output;

        if (seriesId == null && bookId == null)
        {
            _logger.LogWarning("Please specify the series ID or book ID");
            return false;
        }

        if (seriesId != null && bookId != null)
        {
            _logger.LogWarning("Please specify only the series ID or only the book ID, not both!");
            return false;
        }

        if (string.IsNullOrEmpty(output))
            output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "novels");

        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
            _logger.LogInformation("Created output directory {output}", output);
        }

        var books = await GetBooks(seriesId, bookId);
        if (books.Length == 0)
        {
            _logger.LogWarning("Could not find any books for the series ID {seriesId}", seriesId);
            return false;
        }

        var res = await _epub.Generate(books);
        if (res is null)
        {
            _logger.LogWarning("An error occurred while generating epubs.");
            return false;
        }

        var (stream, name, type) = res;
        var path = Path.Combine(output, name);

        if (File.Exists(path))
        {
            _logger.LogWarning("File {path} already exists, overwriting", path);
            File.Delete(path);
        }

        using var io = File.Create(path);
        await stream.CopyToAsync(io, token);
        stream.Close();

        _logger.LogInformation("Generated EPUB file(s) [{type}] {path}", type, path);
        return true;
    }
}
