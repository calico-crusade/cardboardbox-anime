using System.Text.Json.Serialization;

namespace CardboardBox.Anime.AI
{
	public class AiRequest
	{
		[JsonPropertyName("prompt")]
		public string Prompt { get; set; } = string.Empty;

		[JsonPropertyName("negativePrompt")]
		public string NegativePrompt { get; set; } = string.Empty;

		[JsonPropertyName("steps")]
		public long Steps { get; set; } = 32;

		[JsonPropertyName("batchCount")]
		public long BatchCount { get; set; } = 1;

		[JsonPropertyName("batchSize")]
		public long BatchSize { get; set; } = 1;

		[JsonPropertyName("cfgScale")]
		public double CfgScale { get; set; } = 12;

		[JsonPropertyName("seed")]
		public long Seed { get; set; } = -1;

		[JsonPropertyName("height")]
		public long Height { get; set; } = 512;

		[JsonPropertyName("width")]
		public long Width { get; set; } = 512;
	}

	public class AiRequestImg2Img : AiRequest
	{
		[JsonPropertyName("image")]
		public string Image { get; set; } = string.Empty;

		[JsonPropertyName("denoiseStrength")]
		public double DenoiseStrength { get; set; } = 0.7;
	}
}