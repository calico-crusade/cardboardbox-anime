using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json.Serialization;

namespace CardboardBox.Match;

using Http;

public interface INsfwApiService
{
	Task<NsfwResponse?> Get(string url);
}

public class NsfwApiService : INsfwApiService
{
	private readonly IApiService _api;
	private readonly IConfiguration _config;

	public string ApiUrl => _config["NsfwApiUrl"] ?? throw new ArgumentNullException("NsfwApiUrl");

	public NsfwApiService(
		IApiService api, 
		IConfiguration config)
	{
		_api = api;
		_config = config;
	}

	public Task<NsfwResponse?> Get(string url)
	{
		var uri = $"{ApiUrl}/nsfw/{WebUtility.UrlEncode(url)}";
		return _api.Get<NsfwResponse?>(uri);
	}
}

public class NsfwResponse
{
	public const string CLASS_DRAWING = "Drawing";
	public const string CLASS_NUETRAL = "Neutral";
	public const string CLASS_HENTAI = "Hentai";
	public const string CLASS_SEXY = "Sexy";
	public const string CLASS_PORN = "Porn";

	[JsonPropertyName("worked")]
	public bool Worked { get; set; }

	[JsonPropertyName("classifications")]
	public Classification[] Classifications { get; set; } = Array.Empty<Classification>();

	[JsonPropertyName("error")]
	public string? Error { get; set; }

	[JsonIgnore]
	public double? this[string name] => Math.Round((Classifications.FirstOrDefault(x => x.Name.ToLower().Trim() == name.ToLower())?.Probability ?? 0) * 100, 2);

	public class Classification
	{
		[JsonPropertyName("className")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("probability")]
		public double Probability { get; set; }
	}
}
