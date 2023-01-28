using Microsoft.Extensions.Configuration;

namespace CardboardBox.Anime.AI;

using Http;

public interface IAiAnimeService
{
	Task<AiResponse?> Text2Img(AiRequest request);
	Task<AiResponse?> Img2Img(AiRequestImg2Img request);
	Task<string[]> Embeddings();
}

public class AiAnimeService : IAiAnimeService
{
	private readonly IApiService _api;
	private readonly IConfiguration _config;

	public string AIUrl => _config["Ai:Url"];

	public AiAnimeService(
		IApiService api, 
		IConfiguration config)
	{
		_api = api;
		_config = config;
	}

	public Task<AiResponse?> Text2Img(AiRequest request)
	{
		return _api.Post<AiResponse, AiRequest>(AIUrl + "/txt2img", request);
	}

	public Task<AiResponse?> Img2Img(AiRequestImg2Img request)
	{
		return _api.Post<AiResponse, AiRequestImg2Img>(AIUrl + "/img2img", request);
	}

	public async Task<string[]> Embeddings()
	{
		var embeds = await _api.Get<EmbeddingsResponse>(AIUrl + "/embeddings");
		return embeds?.Embeddings ?? Array.Empty<string>();
	}
}
