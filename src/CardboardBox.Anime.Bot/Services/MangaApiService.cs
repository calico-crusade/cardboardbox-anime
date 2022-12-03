using System.Net;

namespace CardboardBox.Anime.Bot.Services
{
	using Core.Models;
	using Database;
	using Manga;

	public interface IMangaApiService
	{
		string ApiUrl { get; }

		Task<PaginatedResult<DbManga>?> Get(int page = 1, int size = 100);
		Task<MangaWithChapters?> GetManga(long id);
		Task<string[]> GetPages(long mangaId, long chapterId);
		Task<MangaWithChapters?> LoadManga(string url, bool force = false);
		Task<PaginatedResult<MangaProgress>?> Search(MangaFilter? filter = null);
		Task<MangaWorked[]?> Update(int count);
		Task<PaginatedResult<MangaProgress>?> Since(DateTime date, int page = 1, int size = 100);
	}

	public class MangaApiService : IMangaApiService
	{
		private static Dictionary<long, CacheItem<MangaWithChapters?>> _mangaCache = new();
		private static Dictionary<string, CacheItem<string[]>> _pagesCache = new();
		private readonly IConfiguration _config;
		private readonly IApiService _api;

		public string ApiUrl => _config["CBA:Url"];

		public MangaApiService(
			IConfiguration config,
			IApiService api)
		{
			_config = config;
			_api = api;
		}

		public Task<PaginatedResult<DbManga>?> Get(int page = 1, int size = 100)
		{
			return _api.Get<PaginatedResult<DbManga>>($"{ApiUrl}/manga?page={page}&size={size}");
		}

		public Task<MangaWithChapters?> GetManga(long id)
		{
			if (!_mangaCache.ContainsKey(id))
				_mangaCache.Add(id, new CacheItem<MangaWithChapters?>(async () => await GetRawManga(id)));

			return _mangaCache[id].Get();
		}

		public Task<MangaWithChapters?> GetRawManga(long id)
		{
			return _api.Get<MangaWithChapters>($"{ApiUrl}/manga/{id}");
		}

		public async Task<string[]> GetPages(long mangaId, long chapterId)
		{
			var key = $"{mangaId}-{chapterId}";
			if (!_pagesCache.ContainsKey(key))
				_pagesCache.Add(key, new CacheItem<string[]>(async () => await GetRawPages(mangaId, chapterId)));

			return await _pagesCache[key].Get() ?? Array.Empty<string>();
		}

		public async Task<string[]> GetRawPages(long mangaId, long chapterId)
		{
			return await _api.Get<string[]>($"{ApiUrl}/manga/{mangaId}/{chapterId}/pages") ?? Array.Empty<string>();
		}

		public Task<MangaWithChapters?> LoadManga(string url, bool force = false)
		{
			url = WebUtility.UrlEncode(url);
			return _api.Get<MangaWithChapters>($"{ApiUrl}/manga/load?url={url}&force={force}");
		}

		public Task<PaginatedResult<MangaProgress>?> Search(MangaFilter? filter = null)
		{
			filter ??= new();
			return _api.Post<PaginatedResult<MangaProgress>, MangaFilter>($"{ApiUrl}/manga/search", filter);
		}

		public Task<MangaWorked[]?> Update(int count)
		{
			return _api.Get<MangaWorked[]>($"{ApiUrl}/manga/refresh?count={count}");
		}

		public Task<PaginatedResult<MangaProgress>?> Since(DateTime date, int page = 1, int size = 100)
		{
			return _api.Get<PaginatedResult<MangaProgress>>($"{ApiUrl}/manga/since/{date:yyyy-MM-ddTHH:mm:ssZ}?page={page}&size={size}");
		}
	}
}
