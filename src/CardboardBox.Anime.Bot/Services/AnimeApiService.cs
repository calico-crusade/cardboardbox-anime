﻿namespace CardboardBox.Anime.Bot.Services;

public interface IAnimeApiService
{
	Task<DbAnime[]> Random(AnimeFilter search);
	Task<PaginatedResult<DbAnime>> Search(AnimeFilter search);
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

	public async Task<DbAnime[]> Random(AnimeFilter search)
	{
		return await _api.Post<DbAnime[], AnimeFilter>($"{ApiUrl}/anime/random", search) ?? Array.Empty<DbAnime>();
	}

	public async Task<PaginatedResult<DbAnime>> Search(AnimeFilter search)
	{
		return await _api.Post<PaginatedResult<DbAnime>, AnimeFilter>($"{ApiUrl}/anime", search)
			?? new PaginatedResult<DbAnime>(0, 0, Array.Empty<DbAnime>());
	}

	public Task<Filter[]?> Filters()
	{
		return _api.Get<Filter[]>($"{ApiUrl}/filters");
	}
}