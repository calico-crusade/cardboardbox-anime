namespace CardboardBox.Anime.Ui.Client.Services;

using Models;

public interface IAnimeService
{
	Task<PaginatedResult<AnimeModel>> Search(FilterSearch search);
	Task<AnimeModel[]> Random(FilterSearch search);
	Task<Filter[]> Filters();
}

public class AnimeService : IAnimeService
{
	private readonly IApiService _api;
	private readonly IConfiguration _config;

	public string BaseUrl => _config["ApiUrl"] ?? throw new ArgumentNullException("ApiUrl");

	public AnimeService(
		IApiService api, 
		IConfiguration config)
	{
		_api = api;
		_config = config;
	}

	public string Url(params string[] parts)
	{
		return string.Join("/", parts.Prepend(BaseUrl));
	}

	public async Task<PaginatedResult<AnimeModel>> Search(FilterSearch search)
	{
		var url = Url("anime");
		return await _api.Post<PaginatedResult<AnimeModel>, FilterSearch>(url, search) 
			?? new PaginatedResult<AnimeModel>(0, 0, []);
	}

	public async Task<AnimeModel[]> Random(FilterSearch search)
	{
		var url = Url("anime", "random");
		return await _api.Post<AnimeModel[], FilterSearch>(url, search) ?? Array.Empty<AnimeModel>();
	}

	public async Task<Filter[]> Filters()
	{
		var url = Url("anime", "filters");
		return await _api.Get<Filter[]>(url) ?? Array.Empty<Filter>();
	}
}

public class FakeCacheService : ICacheService
{
	public Task<T?> Load<T>(string filename)
	{
		return Task.FromResult(default(T));
	}

	public Task Save<T>(T data, string filename)
	{
		return Task.CompletedTask;
	}

	public bool Validate(string uri, out string? filename, string cacheDir = "Cache", double cacheMin = 5)
	{
		filename = null;
		return false;
	}
}
