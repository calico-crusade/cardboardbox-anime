using Google.Cloud.Vision.V1;
using Image = Google.Cloud.Vision.V1.Image;

namespace CardboardBox.Anime.Bot.Services
{
	public interface IGoogleVisionService
	{
		Task<VisionResults?> ExecuteVisionRequest(string imageUrl);
	}

	public class GoogleVisionService : IGoogleVisionService
	{
		private readonly ILogger _logger;

		public GoogleVisionService(ILogger<GoogleVisionService> logger)
		{
			_logger = logger;
		}

		public async Task<VisionResults?> ExecuteVisionRequest(string imageUrl)
		{
			try
			{
				var image = Image.FromUri(imageUrl);
				var client = await ImageAnnotatorClient.CreateAsync();
				var detection = await client.DetectWebInformationAsync(image);

				if (detection.WebEntities.Count == 0 ||
					detection.PagesWithMatchingImages.Count == 0)
					return null;

				var entities = detection.WebEntities.OrderByDescending(t => t.Score).First();
				var guess = entities.Description;
				var score = entities.Score;

				var pages = detection.PagesWithMatchingImages.OrderByDescending(t => t.Score).Select(t => (t.Url, t.PageTitle)).ToArray();

				return new(guess, score, pages);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred during Google Image Vision Request: " + imageUrl);
				return null;
			}
		}
	}

	public record class VisionResults(string Guess, float Score, (string Url, string Title)[] WebPages);
}
