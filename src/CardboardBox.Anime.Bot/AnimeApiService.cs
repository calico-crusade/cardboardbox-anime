using CardboardBox.Anime.Core;
using CardboardBox.Anime.Core.Models;
using CardboardBox.Anime.Database;
using CardboardBox.Http;
using Microsoft.Extensions.Configuration;

namespace CardboardBox.Anime.Bot
{
	public interface IAnimeApiService
	{
		Task<DbAnime[]> Random(FilterSearch search);
		Task<PaginatedResult<DbAnime>> Search(FilterSearch search);
		Task<Filter[]?> Filters();
	}

	public class AnimeApiService : IAnimeApiService
	{
		private readonly IConfiguration _config;
		private readonly IApiService _api;

		public string ApiUrl => _config["CBA:Url"];

		public AnimeApiService(
			IConfiguration config,
			IApiService api)
		{
			_config = config;
			_api = api;
		}

		public async Task<DbAnime[]> Random(FilterSearch search)
		{
			return await _api.Post<DbAnime[], FilterSearch>($"{ApiUrl}/anime/random", search) ?? Array.Empty<DbAnime>();
		}

		public async Task<PaginatedResult<DbAnime>> Search(FilterSearch search)
		{
			return await _api.Post<PaginatedResult<DbAnime>, FilterSearch>($"{ApiUrl}/anime", search)
				?? new PaginatedResult<DbAnime>(0, 0, Array.Empty<DbAnime>());
		}

		public Task<Filter[]?> Filters()
		{
			return _api.Get<Filter[]>($"{ApiUrl}/filters");
		}
	}
}