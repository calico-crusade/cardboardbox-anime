namespace CardboardBox.BackgroundTasks.Cli.Tasks
{
	public class MangaUpdater : IScheduledTask
	{
		public int DelayMs => 60 * 1000 * 3;

		private readonly IMangaService _manga;
		private readonly ILogger _logger;

		public MangaUpdater(
			ILogger<MangaUpdater> logger,
			IMangaService manga)
		{
			_logger = logger;
			_manga = manga;
		}

		public async Task Run()
		{
			try
			{
				_logger.LogInformation("Starting update for manga...");
				var res = await _manga.Updated(2, null);
				if (res == null || res.Length == 0)
				{
					_logger.LogWarning("No results for manga update");
					return;
				}

				foreach (var item in res)
					_logger.LogInformation($"Manga Updated: {item.Manga.Title} :: {item.Worked}");

				_logger.LogInformation("Finished update for manga.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while updating manga");
			}
		}
	}
}
