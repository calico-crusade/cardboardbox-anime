using CardboardBox.Http;
using CardboardBox.Match.SauceNao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CardboardBox.Match;

public interface ISauceNaoApiService
{
	Task<Sauce?> Get(string imageUrl, params SauceNaoDatabase[] databases);

	Task<Sauce?> Get(string imageUrl, int resultCount, params SauceNaoDatabase[] databases);
}

public class SauceNaoApiService : ISauceNaoApiService
{
	private readonly IConfiguration _config;
	private readonly IApiService _api;
	private readonly ILogger _logger;

	public string ApiKey => _config["SauceNao:Token"] ?? throw new ArgumentNullException(nameof(ApiKey));

	public SauceNaoApiService(
		IConfiguration config,
		IApiService api,
		ILogger<SauceNaoApiService> logger)
	{
		_config = config;
		_api = api;
		_logger = logger;
	}

	public Task<Sauce?> Get(string imageUrl, params SauceNaoDatabase[] databases)
	{
		return Get(imageUrl, 6, databases);
	}

	public async Task<Sauce?> Get(string imageUrl, int resultCount, params SauceNaoDatabase[] databases)
	{
		try
		{
			var pars = new Dictionary<string, string>
			{
				["url"] = WebUtility.UrlEncode(imageUrl),
				["output_type"] = "2",
				["api_key"] = ApiKey,
				["numres"] = resultCount.ToString(),
			};

			if (databases.Length == 0) pars.Add("db", "999");

			foreach (var db in databases)
				pars.Add("dbs[]", ((int)db).ToString());

			var url = $"https://saucenao.com/search.php?{string.Join("&", pars.Select(t => $"{t.Key}={t.Value}"))}";
			return await _api.Get<Sauce>(url);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while looking up saucenao image: {image}", imageUrl);
			return null;
		}
	}
}
