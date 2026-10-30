using CardboardBox.Anime.Patreon;
using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("patreon-scrape", HelpText = "Scrape configured Patreon chapters and load them into the database")]
public sealed class PatreonScrapeOptions
{
	[Option('d', "directory", HelpText = "Root directory containing the series folders and meta.txt files")]
	public string? Directory { get; set; }

	[Option("profile", HelpText = "Dedicated browser profile directory used to retain the Patreon login")]
	public string? BrowserProfileDirectory { get; set; }

	[Option("browser-channel", HelpText = "Optional installed browser channel, such as msedge or chrome")]
	public string? BrowserChannel { get; set; }

	[Option("login-browser", HelpText = "Path to a regular Chrome or Edge executable used for interactive login")]
	public string? LoginBrowserExecutablePath { get; set; }

	[Option("headless", HelpText = "Run without a visible browser (requires a previously authenticated profile)", Default = false)]
	public bool Headless { get; set; }

	[Option("wait-for-login", HelpText = "Open a regular browser for interactive Patreon login before scraping", Default = false)]
	public bool WaitForLogin { get; set; }

	[Option("max-scrolls", HelpText = "Maximum listing-page scrolls per series", Default = 25)]
	public int MaxScrolls { get; set; } = 25;

	[Option("max-posts", HelpText = "Maximum candidate posts inspected per series", Default = 100)]
	public int MaxPosts { get; set; } = 10;

	[Option("login-timeout", HelpText = "Minutes to wait for manual Patreon login", Default = 10)]
	public int LoginTimeoutMinutes { get; set; } = 10;
}

internal sealed class PatreonScrapeVerb(
	IPatreonScraper scraper,
	ManualPatreonLoadVerb loader,
	ILogger<PatreonScrapeVerb> logger) : BooleanVerb<PatreonScrapeOptions>(logger)
{
	public override async Task<bool> Execute(PatreonScrapeOptions options, CancellationToken token)
	{
		var directory = ManualPatreonLoadVerb.DetermineDirectory(options.Directory);
		if (directory is null)
		{
			_logger.LogError("The Patreon load directory does not exist: {Directory}", options.Directory);
			return false;
		}

		if (options.MaxScrolls < 0 || options.MaxPosts <= 0 || options.LoginTimeoutMinutes <= 0)
		{
			_logger.LogError("max-scrolls must be non-negative; max-posts and login-timeout must be positive.");
			return false;
		}

		PatreonScrapeResult scrapeResult;
		try
		{
			scrapeResult = await scraper.ScrapeAsync(new CardboardBox.Anime.Patreon.PatreonScrapeOptions
			{
				RootDirectory = directory,
				BrowserProfileDirectory = options.BrowserProfileDirectory,
				BrowserChannel = options.BrowserChannel,
				LoginBrowserExecutablePath = options.LoginBrowserExecutablePath,
				Headless = options.Headless,
				WaitForLogin = options.WaitForLogin,
				MaxScrolls = options.MaxScrolls,
				MaxPostsPerSeries = options.MaxPosts,
				LoginTimeout = TimeSpan.FromMinutes(options.LoginTimeoutMinutes),
				Progress = progress => _logger.LogInformation(
					"Patreon: {Message} [{SeriesDirectory}] {PostUrl}",
					progress.Message,
					progress.SeriesDirectory,
					progress.PostUrl),
			}, token);
		}
		catch (PatreonAuthenticationException ex)
		{
			_logger.LogError("{AuthenticationError}", ex.Message);
			return false;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Patreon scraping could not start.");
			return false;
		}

		foreach (var series in scrapeResult.Series)
		{
			if (series.Error is not null)
			{
				_logger.LogError(
					"Failed to scrape {Directory} ({SourceUrl}): {Error}",
					series.Directory,
					series.SourceUrl,
					series.Error);
				continue;
			}

			_logger.LogInformation(
				"Scraped {Directory}: wrote {Written} chapter files and skipped {Skipped} posts.",
				series.Directory,
				series.FilesWritten.Count,
				series.PostsSkipped);
		}

		_logger.LogInformation(
			"Patreon scrape finished with {FilesWritten} new files.",
			scrapeResult.FilesWritten);

		var loaded = true;
		foreach (var series in scrapeResult.Series.Where(series => series.Error is null && series.FilesWritten.Count > 0))
		{
			var folderName = Path.GetFileName(series.Directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			var seriesIdText = folderName.Split('-').FirstOrDefault();
			if (!int.TryParse(seriesIdText, out var seriesId))
			{
				_logger.LogError("Cannot import new Patreon files because {Directory} has no numeric series ID.", series.Directory);
				loaded = false;
				continue;
			}

			_logger.LogInformation(
				"Loading only {Count} newly scraped files for series {SeriesId}.",
				series.FilesWritten.Count,
				seriesId);
			var files = series.FilesWritten.ToHashSet(StringComparer.OrdinalIgnoreCase);
			loaded &= await loader.ProcessSeries(seriesId, series.Directory, token, files);
		}

		if (scrapeResult.FilesWritten == 0)
			_logger.LogInformation("No new Patreon chapters were found; the database loader was not run.");

		return scrapeResult.Succeeded && loaded;
	}
}
