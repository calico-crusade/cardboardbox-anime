namespace CardboardBox.BackgroundTasks.Cli.Tasks
{
	using Anime.Database;

	public class ReverseImageSearchIndexing : IScheduledTask
	{
		private readonly IMangaMatchService _match;
		private readonly IMangaCacheDbService _db;
		private readonly ILogger _logger;

		public int DelayMs => 30 * 1000;

		public ReverseImageSearchIndexing(
			IMangaMatchService match, 
			ILogger<ReverseImageSearchIndexing> logger,
			IMangaCacheDbService db)
		{
			_match = match;
			_logger = logger;
			_db = db;
		}

		public async Task Run()
		{
			try
			{
				_logger.LogInformation("Starting indexing...");
				await _match.IndexLatest();
				_logger.LogInformation("Finished indexing. Starting update merger...");
				await _db.MergeUpdates();
				_logger.LogInformation("Finished update merger.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing reverse image search");
			}
		}
	}
}
