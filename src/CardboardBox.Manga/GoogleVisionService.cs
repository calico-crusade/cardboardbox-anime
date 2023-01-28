using Google.Cloud.Vision.V1;
using Image = Google.Cloud.Vision.V1.Image;

namespace CardboardBox.Manga;

public interface IGoogleVisionService
{
	Task<VisionResults?> ExecuteVisionRequest(string imageUrl);
	Task<VisionResults?> ExecuteVisionRequest(Stream stream, string name);
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
		var image = Image.FromUri(imageUrl);
		return await ExecuteVisionRequest(image, imageUrl);
	}

	public async Task<VisionResults?> ExecuteVisionRequest(Stream stream, string name)
	{
		var image = await Image.FromStreamAsync(stream);
		return await ExecuteVisionRequest(image, name);
	}

	public async Task<VisionResults?> ExecuteVisionRequest(Image image, string name)
	{
		try
		{
			var client = await ImageAnnotatorClient.CreateAsync();
			var detection = await client.DetectWebInformationAsync(image);

			if (detection.WebEntities.Count == 0 ||
				detection.PagesWithMatchingImages.Count == 0)
				return null;

			var entities = detection
				.WebEntities
				.OrderByDescending(t => t.Score)
				.First();
			var guess = entities.Description;
			var score = entities.Score;

			var pages = detection
				.PagesWithMatchingImages
				.OrderByDescending(t => t.PageTitle.Length)
				.Select(t => (t.Url, t.PageTitle))
				.ToArray();

			return new(guess, score, pages);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during Google Image Vision Request: " + name);
			return null;
		}
	}
}

public record class VisionResults(string Guess, float Score, (string Url, string Title)[] WebPages);
