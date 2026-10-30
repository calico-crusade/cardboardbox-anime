namespace CardboardBox.Anime.Patreon;

public interface IPatreonScraper
{
	Task<PatreonScrapeResult> ScrapeAsync(PatreonScrapeOptions options, CancellationToken token = default);
}

public sealed class PatreonScrapeOptions
{
	public required string RootDirectory { get; init; }

	public string? BrowserProfileDirectory { get; init; }

	public string? BrowserChannel { get; init; }

	public string? LoginBrowserExecutablePath { get; init; }

	public bool Headless { get; init; }

	public bool WaitForLogin { get; init; }

	public int MaxScrolls { get; init; } = 25;

	public int MaxPostsPerSeries { get; init; } = 100;

	public TimeSpan LoginTimeout { get; init; } = TimeSpan.FromMinutes(10);

	public Action<PatreonScrapeProgress>? Progress { get; init; }
}

public sealed record PatreonScrapeProgress(
	string Message,
	string? SeriesDirectory = null,
	string? PostUrl = null);

public sealed record PatreonScrapeResult(IReadOnlyList<PatreonSeriesScrapeResult> Series)
{
	public int FilesWritten => Series.Sum(series => series.FilesWritten.Count);

	public bool Succeeded => Series.All(series => series.Error is null);
}

public sealed record PatreonSeriesScrapeResult(
	string Directory,
	string? SourceUrl,
	IReadOnlyList<string> FilesWritten,
	int PostsSkipped,
	string? Error = null);

public sealed class PatreonAuthenticationException(string message) : InvalidOperationException(message);
