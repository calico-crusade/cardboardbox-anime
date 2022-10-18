namespace CardboardBox.Anime.AI
{
	using Http;

	public interface IAiAnimeService
	{
		Task<AiResponse?> Get(AiRequest request);
	}

	public class AiAnimeService : IAiAnimeService
	{
		private readonly IApiService _api;

		public AiAnimeService(IApiService api)
		{
			_api = api;
		}

		public Task<AiResponse?> Get(AiRequest request)
		{
			return _api.Post<AiResponse, AiRequest>("http://127.0.0.1:5000/txt2img", request);
		}
	}
}
