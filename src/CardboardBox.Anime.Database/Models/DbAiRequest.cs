namespace CardboardBox.Anime.Database;

public class DbAiRequest : DbObject
{
	public long ProfileId { get; set; }

	public string Prompt { get; set; } = string.Empty;

	public string NegativePrompt { get; set; } = string.Empty;

	public long Steps { get; set; }

	public long BatchCount { get; set; }

	public long BatchSize { get; set; }

	public double CfgScale { get; set; }

	public long Seed { get; set; }

	public long Height { get; set; }

	public long Width { get; set; }

	public string? ImageUrl { get; set; }

	public double? DenoiseStrength { get; set; }

	public string[] OutputPaths { get; set; } = Array.Empty<string>();

	public DateTime GenerationStart { get; set; }

	public DateTime? GenerationEnd { get; set; }

	public long? SecondsElapsed { get; set; }

	public string Sampler { get; set; } = "Euler";
}
