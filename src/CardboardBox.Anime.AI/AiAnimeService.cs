namespace CardboardBox.Anime.AI
{
	using Http;

	public interface IAiAnimeService
	{
		Task<AiResponse?> Text2Img(AiRequest request);
		Task<AiResponse?> Img2Img(AiRequestImg2Img request);
		Task<string[]> Embeddings();
	}

	public class AiAnimeService : IAiAnimeService
	{
		private readonly string _rootUrl = "http://127.0.0.1:5000";

		private readonly IApiService _api;

		public AiAnimeService(IApiService api)
		{
			_api = api;
		}

		public Task<AiResponse?> Text2Img(AiRequest request)
		{
			return _api.Post<AiResponse, AiRequest>(_rootUrl + "/txt2img", request);
		}

		public Task<AiResponse?> Img2Img(AiRequestImg2Img request)
		{
			return _api.Post<AiResponse, AiRequestImg2Img>(_rootUrl + "/img2img", request);
		}

		public async Task<string[]> Embeddings()
		{
			var embeds = await _api.Get<EmbeddingsResponse>(_rootUrl + "/embeddings");
			return embeds?.Embeddings ?? Array.Empty<string>();
		}
	}
}
