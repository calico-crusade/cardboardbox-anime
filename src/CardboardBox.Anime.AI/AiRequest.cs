using System.Text.Json.Serialization;

namespace CardboardBox.Anime.AI;

public class AiRequest
{
	[JsonPropertyName("prompt")]
	public string Prompt { get; set; } = string.Empty;

	[JsonPropertyName("negative_prompt")]
	public string NegativePrompt { get; set; } = string.Empty;

	[JsonPropertyName("steps")]
	public long Steps { get; set; } = 32;

	[JsonPropertyName("n_iter")]
	public long BatchCount { get; set; } = 1;

	[JsonPropertyName("batch_size")]
	public long BatchSize { get; set; } = 1;

	[JsonPropertyName("cfg_scale")]
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
	[JsonIgnore]
	public string? Image
	{
		get => Images.FirstOrDefault();
		set => Images = string.IsNullOrEmpty(value) ? Array.Empty<string>() : new[] { value };
	}

	[JsonPropertyName("init_images")]
	public string[] Images { get; set; } = Array.Empty<string>();

	[JsonPropertyName("denoising_strength")]
	public double DenoiseStrength { get; set; } = 0.7;
}