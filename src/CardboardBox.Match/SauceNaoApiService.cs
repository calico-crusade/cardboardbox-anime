using CardboardBox.Http;
using CardboardBox.Match.SauceNao;
using Microsoft.Extensions.Configuration;
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

	public string ApiKey => _config["SauceNao:Token"];

	public SauceNaoApiService(
		IConfiguration config,
		IApiService api)
	{
		_config = config;
		_api = api;
	}

	public Task<Sauce?> Get(string imageUrl, params SauceNaoDatabase[] databases)
	{
		return Get(imageUrl, 6, databases);
	}

	public Task<Sauce?> Get(string imageUrl, int resultCount, params SauceNaoDatabase[] databases)
	{
		var pars = new Dictionary<string, string>
		{
			["url"] = WebUtility.UrlEncode(imageUrl),
			["output_type"] = "2",
			["api_key"] = ApiKey,
			["numres"] = resultCount.ToString(),
		};

		if (databases.Length == 0) pars.Add("db", "999");
		
		foreach(var db in databases)
			pars.Add("dbs[]", ((int)db).ToString());

		var url = $"https://saucenao.com/search.php?{string.Join("&", pars.Select(t => $"{t.Key}={t.Value}"))}";
		return _api.Get<Sauce>(url);
	}
}
