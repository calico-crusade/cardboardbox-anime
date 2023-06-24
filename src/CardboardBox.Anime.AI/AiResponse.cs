using System.Text.Json.Serialization;

namespace CardboardBox.Anime.AI;

public class AiResponse
{
	[JsonPropertyName("images")]
	public string[] Images { get; set; } = Array.Empty<string>();
}

public class EmbeddingsResponse
{
	[JsonIgnore]
	public string[] Embeddings => Loaded.Keys.ToArray();

	[JsonPropertyName("loaded")]
	public Dictionary<string, Embedding> Loaded { get; set; } = new();

	public class Embedding
	{
		[JsonPropertyName("step")]
		public int? Step { get; set; }

		[JsonPropertyName("sd_checkpoint")]
		public string? CheckpointId { get; set; }

		[JsonPropertyName("sd_checkpoint_name")]
		public string? CheckpointName { get; set; }

		[JsonPropertyName("shape")]
		public int? Shape { get; set; }

		[JsonPropertyName("vectors")]
		public int? Vectors { get; set; }
	}
}

public class LoraResponse
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("alias")]
	public string Alias { get; set; } = string.Empty;

	[JsonPropertyName("path")]
	public string Path { get; set; } = string.Empty;
}

public class SamplerResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("aliases")]
    public string[] Aliases { get; set; } = Array.Empty<string>();
}