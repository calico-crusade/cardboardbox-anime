using MangaDexSharp;
using MManga = MangaDexSharp.Manga;

namespace CardboardBox.BackgroundTasks.Cli.Tasks;

using Anime.Database;
using Manga.MangaDex;

public class HistoricalMangadexIndexing : IScheduledTask
{
	private const string TRACKER_NAME = "historical_check.json";
	private const string LOGGER_NAME = "Manga Historical Indexing";
	private readonly IMangaMatchService _match;
	private readonly IMangaCacheDbService _db;
	private readonly ILogger _logger;
	private readonly IMangaDexService _md;

	private DateTime? _earliest;
	private readonly int _restAfter = 10;
	private readonly int _restForSec = 60;
	private int _requestCount = 0;

	public DateTime Earliest
	{
		get => _earliest ??= GetEarliest();
		set => SetEarliest((_earliest = value).Value);
	}

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
			_logger.LogInformation($"{LOGGER_NAME}: Starting Mangadex Historical Indexing...");
			var current = await GetManga(Earliest);
			if (current == null || current.Data.Count == 0)
			{
				_logger.LogError($"{LOGGER_NAME}: Couldn't fetch manga for: \"{Earliest}\"");
				return;
			}

			while (current.Total > 0 || current.Data.Count == 0)
			{
				foreach (var manga in current.Data)
					await HandleManga(manga);

				Earliest = current.Data.Max(t => t.Attributes.CreatedAt);
				current = await GetManga(Earliest);
				await CheckCount($"Manga Rate Limit >> {Earliest}");

				if (current == null)
				{
					_logger.LogError($"{LOGGER_NAME}: Couldn't fetch manga for: \"{Earliest}\"");
					return;
				}

				var perc = (double)(current.Offset + current.Limit) / current.Total * 100;
				_logger.LogDebug($"{LOGGER_NAME}: Progress Report: {current.Offset + current.Limit} / {current.Total} ({perc:0.00}%)");
			}

			_logger.LogInformation($"{LOGGER_NAME}: Finished Mangadex Historical Indexing.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"{LOGGER_NAME}: Error occurred while processing historical mangadex indexing.");
		}
	}

	public DateTime GetEarliest()
	{
		if (!File.Exists(TRACKER_NAME))
			return new DateTime(2001, 1, 1);

		var data = File.ReadAllText(TRACKER_NAME);
		return JsonSerializer.Deserialize<DateTime>(data);
	}

	public void SetEarliest(DateTime value)
	{
		File.WriteAllText(TRACKER_NAME, JsonSerializer.Serialize(value));
	}

	public Task<MangaList> GetManga(DateTime earliest)
	{
		var filter = new MangaFilter
		{
			CreatedAtSince = earliest,
			Order = new() { [MangaFilter.OrderKey.createdAt] = OrderValue.asc },
			AvailableTranslatedLanguage = new[] { "en" }
		};
		return _md.Search(filter);
	}
	
	public async IAsyncEnumerable<Chapter> GetAllChapters(string id)
	{
		Task<ChapterList> GetChapter(int offset)
		{
			return _md.Chapters(id, new MangaFeedFilter
			{
				Limit = 500,
				Offset = offset,
				IncludeExternalUrl = false,
				IncludeFuturePublishAt = false,
				IncludeEmptyPages = false,
				IncludeFutureUpdates = false,
				TranslatedLanguage = new[] { "en" },
			});
		}

		int offset = 0;
		while (true)
		{
			await CheckCount($"Chapter Rate Limit >> {id} + {offset}");
			var chapters = await GetChapter(offset);
			_requestCount++;
			if (chapters == null) yield break;

			foreach (var chap in chapters.Data)
				yield return chap;

			if (chapters.Total >= chapters.Offset + chapters.Limit)
				break;

			offset += chapters.Limit;
		}
	}

	public async Task HandleManga(MManga manga)
	{
		var title = manga.Attributes.Title.PreferedOrFirst(t => t.Key == "en").Value;
		_logger.LogDebug($"{LOGGER_NAME}: Processing Manga >> {title} ({manga.Id})");
		await foreach(var chap in GetAllChapters(manga.Id))
		{
			await CheckCount($"{title} ({manga.Id}) >> {chap.Attributes.Title ?? chap.Attributes.Chapter} ({chap.Id})");
			var pages = await _md.Pages(chap.Id);
			_requestCount++;

			if (pages == null || pages.Images.Length == 0)
			{
				_logger.LogWarning($"{LOGGER_NAME}: Couldn't find any pages for chapter: {chap.Id}");
				continue;
			}

			var (dbChap, dbManga) = await _match.Convert(chap, manga, pages.Images);

			var chunks = dbChap.Pages
				.Select((url, i) => new MangaMetadata
				{
					Id = url.MD5Hash(),
					Source = "mangadex",
					Url = url,
					Type = MangaMetadataType.Page,
					MangaId = manga.Id,
					ChapterId = chap.Id,
					Page = i + 1
				})
				.Split(5);

			foreach(var chunk in chunks)
			{
				var tasks = chunk.Select(t => _match.IndexPage(t.Url, t));
				await Task.WhenAll(tasks);
			}
		}
	}

	public async Task CheckCount(string context)
	{
		if (_requestCount < _restAfter) return;

		_logger.LogDebug($"{LOGGER_NAME}: Delaying indexing due to rate-limits >> {context}");
		await Task.Delay(_restForSec * 1000);
		_requestCount = 0;
	}
}
