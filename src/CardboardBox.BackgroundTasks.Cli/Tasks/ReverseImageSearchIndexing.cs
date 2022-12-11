namespace CardboardBox.BackgroundTasks.Cli.Tasks
{
	public class ReverseImageSearchIndexing : IScheduledTask
	{
		private readonly IMangaMatchService _match;
		private readonly ILogger _logger;

		public int DelayMs => 30 * 1000;

		public ReverseImageSearchIndexing(
			IMangaMatchService match, 
			ILogger<ReverseImageSearchIndexing> logger)
		{
			_match = match;
			_logger = logger;
		}

		public async Task Run()
		{
			_logger.LogInformation("Starting indexing...");
			await _match.IndexLatest();
			_logger.LogInformation("Finished indexing.");
		}
	}
}
