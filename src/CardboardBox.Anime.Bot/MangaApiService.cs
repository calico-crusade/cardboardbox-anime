using System.Net;

namespace CardboardBox.Anime.Bot
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
		Task<PaginatedResult<DbManga>?> Search(MangaFilter? filter = null);
	}

	public class MangaApiService : IMangaApiService
	{
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
			return _api.Get<MangaWithChapters>($"{ApiUrl}/manga/{id}");
		}

		public async Task<string[]> GetPages(long mangaId, long chapterId)
		{
			return await _api.Get<string[]>($"{ApiUrl}/manga/{mangaId}/{chapterId}/pages") ?? Array.Empty<string>();
		}

		public Task<MangaWithChapters?> LoadManga(string url, bool force = false)
		{
			url = WebUtility.UrlEncode(url);
			return _api.Get<MangaWithChapters>($"{ApiUrl}/manga/load?url={url}&force={force}");
		}

		public Task<PaginatedResult<DbManga>?> Search(MangaFilter? filter = null)
		{
			filter ??= new();
			return _api.Post<PaginatedResult<DbManga>, MangaFilter>($"{ApiUrl}/manga/search", filter);
		}
	}
}
