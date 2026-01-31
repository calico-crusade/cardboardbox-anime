using CardboardBox.Http;
using CardboardBox.Json;
using CardboardBox.Match.SauceNao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CardboardBox.Match;

public interface ISauceNaoApiService
{
	Task<Sauce?> Get(string imageUrl, params SauceNaoDatabase[] databases);

	Task<Sauce?> Get(string imageUrl, int resultCount, params SauceNaoDatabase[] databases);

	Task<Sauce?> Get(Stream file, string filename, params SauceNaoDatabase[] databases);

    Task<Sauce?> Get(Stream file, string filename, int resultCount, params SauceNaoDatabase[] databases);
}

public class SauceNaoApiService : ISauceNaoApiService
{
	public const int DEFAULT_RESULT_COUNT = 6;

	private readonly IConfiguration _config;
	private readonly IApiService _api;
	private readonly ILogger _logger;
	private readonly IJsonService _json;

	public string ApiKey => _config["SauceNao:Token"] ?? throw new ArgumentNullException(nameof(ApiKey));

	public SauceNaoApiService(
		IConfiguration config,
		IApiService api,
		IJsonService json,
		ILogger<SauceNaoApiService> logger)
	{
		_config = config;
		_api = api;
		_logger = logger;
		_json = json;
	}

	public Task<Sauce?> Get(string imageUrl, params SauceNaoDatabase[] databases)
	{
		return Get(imageUrl, DEFAULT_RESULT_COUNT, databases);
	}

	public Task<Sauce?> Get(Stream file, string filename, params SauceNaoDatabase[] databases)
	{
		return Get(file, filename, DEFAULT_RESULT_COUNT, databases);
	}

	public Dictionary<string, string> GetParameters(int resultCount, SauceNaoDatabase[] databases)
	{
        var pars = new Dictionary<string, string>
        {
            ["output_type"] = "2",
            ["api_key"] = ApiKey,
            ["numres"] = resultCount.ToString(),
        };

        if (databases.Length == 0) pars.Add("db", "999");

        foreach (var db in databases)
            pars.Add("dbs[]", ((int)db).ToString());

		return pars;
    }

	public string GetUrl(Dictionary<string, string> pars)
	{
        return $"https://saucenao.com/search.php?{string.Join("&", pars.Select(t => $"{t.Key}={t.Value}"))}";
    }

	public async Task<Sauce?> Get(Stream file, string filename, int resultCount, params SauceNaoDatabase[] databases)
	{
		try
        {
            var pars = GetParameters(resultCount, databases);
            var url = GetUrl(pars);
            using var content = new StreamContent(file);
			using var body = new MultipartFormDataContent
			{
				{ content, "file", filename }
			};

			var result = await ((IHttpBuilder)_api
				.Create(url, _json, "POST")
				.BodyContent(body))
				.Result();

			if (result is null || !result.IsSuccessStatusCode)
			{
				string? badContent = null;
				if (result is not null)
					badContent = await result.Content.ReadAsStringAsync();

				_logger.LogError("Error occurred while processing saucenao response: {code} - {content}", result?.StatusCode, badContent);
				return null;
			}

			using var resStream = await result.Content.ReadAsStreamAsync();
			var output = await _json.Deserialize<Sauce>(resStream);
			return output;
		}
		catch (Exception ex)
		{
            _logger.LogError(ex, "Error occurred while looking up saucenao image from stream: {filename}", filename);
			return null;
        }
	}

	public async Task<Sauce?> Get(string imageUrl, int resultCount, params SauceNaoDatabase[] databases)
	{
		try
		{
			var pars = GetParameters(resultCount, databases);
			pars["url"] = WebUtility.UrlEncode(imageUrl);
			var url = GetUrl(pars);
			return await _api.Get<Sauce>(url);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while looking up saucenao image: {image}", imageUrl);
			return null;
		}
	}
}
