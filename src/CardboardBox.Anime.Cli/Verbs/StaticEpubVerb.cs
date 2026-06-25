using CardboardBox.Epub;
using CardboardBox.Extensions;
using CardboardBox.LightNovel.Core;
using CardboardBox.LightNovel.Core.Sources.Utilities;
using CommandLine;
using System.IO.Enumeration;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("static-epub", HelpText = "Generates an epub from the given local directory")]
internal class StaticEpubOptions
{
	public static readonly string[] DEFAULT_PATTERNS = ["*.html", "*.md"];

	[Option('d', "directory", Required = true, HelpText = "The directory to find the files in")]
	public string Directory { get; set; } = string.Empty;

	[Option('t', "title", Required = true, HelpText = "The title to use for the novel")]
	public string Title { get; set; } = string.Empty;

	[Option('p', "patterns", HelpText = "The patterns to match for the directories to load")]
	public IEnumerable<string> Patterns { get; set; } = [];

	[Option('f', "file-patterns", HelpText = "The patterns to match for the files to load")]
	public IEnumerable<string> FilePatterns { get; set; } = DEFAULT_PATTERNS;

	[Option('c', "cover", HelpText = "The cover image to use for the novel")]
	public string? Cover { get; set; }
}

internal class StaticEpubVerb(
	INovelEpubService _epub,
	IMarkdownService _markdown,
	ISmartReaderService _reader,
	ILogger<StaticEpubVerb> logger) : BooleanVerb<StaticEpubOptions>(logger)
{
	public NovelEpubService ProperEpub => (NovelEpubService)_epub;

	public IEnumerable<string> GetEntries(StaticEpubOptions options)
	{
		if (!Directory.Exists(options.Directory))
		{
			_logger.LogError("LMK location does not exist: {Location}", options.Directory);
			yield break;
		}

		var filePatterns = options.FilePatterns is null || !options.FilePatterns.Any() 
			? StaticEpubOptions.DEFAULT_PATTERNS : options.FilePatterns;

		var directories = Directory.GetDirectories(options.Directory);
		foreach(var dir in directories)
		{
			var any = options.Patterns is null || 
				!options.Patterns.Any() || 
				options.Patterns.Any(e => FileSystemName.MatchesWin32Expression(e, dir, true));
			if (!any) continue;

			var files = filePatterns.SelectMany(t => Directory.GetFiles(dir, t, SearchOption.AllDirectories));
			foreach(var file in files)
				yield return file;
		}
	}

	public static int DetermineVolumeOrder(string filename)
	{
		const int DEFAULT = 0;

		if (!filename.ContainsIc("volume")) return DEFAULT;

		var parts = filename.Split(["volume"], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2) return DEFAULT;

		var next = parts[1].Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
		if (string.IsNullOrEmpty(next)) return DEFAULT;

		if (!int.TryParse(next, out var volume)) return DEFAULT;

		return volume;
	}

	public static int DeterminePrologueOrder(string filename)
	{
		const int DEFAULT = 0;
		var markers = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
		{
			["prologue"] = -1,
		};
	
		foreach(var marker in markers)
		{
			if (filename.Contains(marker.Key, StringComparison.InvariantCultureIgnoreCase))
				return marker.Value;
		}

		return DEFAULT;
	}

	public static IEnumerable<string> OrderEntries(IEnumerable<string> entries)
	{
		return entries
			.OrderBy(DetermineVolumeOrder)
			.ThenBy(DeterminePrologueOrder)
			.ThenBy(t => t, StringComparer.InvariantCultureIgnoreCase);
	}

	public async Task GenerateEpub(StaticEpubOptions options, CancellationToken token)
	{
		var entries = OrderEntries(GetEntries(options));
		var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		var outputName = options.Title.PurgePathChars().ToLower().Replace(" ", "-");
		var output = Path.Combine(desktop, $"{outputName}.epub");
		await using var epub = EpubBuilder.Create(options.Title, output);
		var bob = await epub.Start();
		bob.BelongsTo(options.Title, 1);
		await bob.AddStylesheetFromFile("stylesheet.css", "stylesheet.css");

		if (!string.IsNullOrEmpty(options.Cover) && File.Exists(options.Cover))
		{
			var filename = Path.GetFileName(options.Cover);
			using var io = File.OpenRead(options.Cover);
			await bob.AddCoverImage(filename, io);
		}

		int p = 0;
		foreach(var entry in entries)
		{
			var ext = Path.GetExtension(entry).TrimStart('.').ToLower();
			_logger.LogInformation("Loading Epub entry: {Entry}", entry);
			var rawContent = await File.ReadAllTextAsync(entry, token);
			string? title = $"Chapter {p + 1}";
			string? content = string.Empty;

			switch(ext)
			{
				case "html":
					var (t, c) = await _reader.GetCleanArticle(rawContent, entry);
					title = t ?? title;
					content = c;
					break;
				case "md":
					var html = _markdown.ToHtml(rawContent);
					content = _reader.CleanseHtml(html, entry);
					break;
				default:
					_logger.LogWarning("Unsupported file type for LMK entry: {Entry}", entry);
					continue;
			}
			
			if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(title))
			{
				_logger.LogWarning("Epub entry has no content or title: {Entry} >> {Title}", entry, title);
				continue;
			}
			p++;
			await bob.AddChapter(title, async c =>
			{
				var header = $"<h1>{title}</h1>";
				var chapterContent = $"{header}{ProperEpub.CleanContents(content, title)}";
				await ProperEpub.PostFixImages(bob, c, $"chapter-{p}.xhtml", chapterContent);
			});
			_logger.LogInformation("Added Epub entry: {Entry} >> {Title}", entry, title);
		}
		_logger.LogInformation("Finished loading EPUB entries. Output: {Output}", output);
	}

	public override async Task<bool> Execute(StaticEpubOptions options, CancellationToken token)
	{
		await GenerateEpub(options, token);
		return true;
	}
}
