namespace CardboardBox.BackgroundTasks.Cli.Tasks;

using Anime.Database;
using Manga.MangaDex;
using Manga.MangaDex.Models;

public class HistoricalMangadexIndexing : IScheduledTask
{
	private readonly IMangaMatchService _match;
	private readonly IMangaCacheDbService _db;
	private readonly ILogger _logger;
	private readonly IMangaDexService _md;

	public int DelayMs => 10 * 1000;

	public HistoricalMangadexIndexing(
		IMangaMatchService match, 
		IMangaCacheDbService db, 
		ILogger<HistoricalMangadexIndexing> logger,
		IMangaDexService md)
	{
		_match = match;
		_db = db;
		_logger = logger;
		_md = md;
	}

	public async Task Run()
	{
		try
		{
			_logger.LogInformation("Starting Mangadex Historical Indexing...");



			_logger.LogInformation("Finished Mangadex Historical Indexing.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while processing historical mangadex indexing.");
		}
	}

	public Task<MangaDexCollection<MangaDexManga>?> GetManga(DateTime earliest)
	{
		var filter = new MangaFilter
		{
			CreatedAtSince = earliest,
			Order = new() { [MangaFilter.OrderKey.createdAt] = MangaFilter.OrderValue.asc },
			AvailableTranslatedLanguage = new[] { "en" }
		};
		return _md.Search(filter);
	}
}
