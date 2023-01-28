namespace CardboardBox.LightNovel.Core;

public class Series : HashBase
{
	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty;

	[JsonPropertyName("lastChapterUrl")]
	public string LastChapterUrl { get; set; } = string.Empty;

	[JsonPropertyName("description")]
	public string? Description { get; set; }

	[JsonPropertyName("image")]
	public string? Image { get; set; }

	[JsonPropertyName("genre")]
	public string[] Genre { get; set; } = Array.Empty<string>();

	[JsonPropertyName("tags")]
	public string[] Tags { get; set; } = Array.Empty<string>();

	[JsonPropertyName("authors")]
	public string[] Authors { get; set; } = Array.Empty<string>();

	[JsonPropertyName("illustrators")]
	public string[] Illustrators { get; set; } = Array.Empty<string>();

	[JsonPropertyName("editors")]
	public string[] Editors { get; set; } = Array.Empty<string>();

	[JsonPropertyName("translators")]
	public string[] Translators { get; set; } = Array.Empty<string>();
}
