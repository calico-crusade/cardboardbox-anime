using Microsoft.Extensions.Logging;

namespace CardboardBox.Anime.Bot.Cli
{
	public class MangaUpdater
	{
		private readonly IMangaApiService _api;
		private readonly ILogger _logger;

		public MangaUpdater(
			IMangaApiService api,
			ILogger<MangaUpdater> logger)
		{
			_api = api;
			_logger = logger;
		}

		public async Task Update()
		{
			try
			{
				_logger.LogInformation("Starting manga updater...");

				var res = await _api.Update(5);
				if (res == null || res.Length == 0)
				{
					_logger.LogWarning("No results for manga update");
					return;
				}

				foreach(var item in res)
					_logger.LogInformation($"Manga Updated: {item.Manga.Title} :: {item.Worked}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing manga update");
			}
		}
	}
}
