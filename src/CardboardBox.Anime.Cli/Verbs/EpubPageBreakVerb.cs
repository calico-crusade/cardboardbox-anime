using CardboardBox.LightNovel.Core;
using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("epub-page-break", HelpText = "Inserts page breaks into an EPUB file.")]
internal class EpubPageBreakVerbOptions
{
    [Option('i', "input", Required = true, HelpText = "The input EPUB file path.")]
    public string? Input { get; set; }

    [Option('o', "output", HelpText = "The output EPUB file path.")]
    public string? Output { get; set; }
}

internal class EpubPageBreakVerb(
    ILogger<EpubPageBreakVerb> logger) : BooleanVerb<EpubPageBreakVerbOptions>(logger)
{
    public override async Task<bool> Execute(EpubPageBreakVerbOptions options, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(options.Input))
        {
            logger.LogError("Input file path is not specified.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Output))
            options.Output = Path.Combine(
                Path.GetDirectoryName(options.Input) ?? string.Empty, 
                Path.GetFileNameWithoutExtension(options.Input) + "_modified.epub");

        EpubPageBreakWriter.AddApproximatePageBreaks(options.Input, options.Output, 350);
        _logger.LogInformation("Page breaks added to EPUB file: {Output}", options.Output);
        return true;
    }
}