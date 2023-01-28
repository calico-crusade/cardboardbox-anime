namespace CardboardBox.LightNovel.Core;

public class Book : BookBase
{
	[JsonPropertyName("seriesId")]
	public long SeriesId { get; set; }

	[JsonPropertyName("coverImage")]
	public string? CoverImage { get; set; }

	[JsonPropertyName("forwards")]
	public string[] Forwards { get; set; } = Array.Empty<string>();

	[JsonPropertyName("inserts")]
	public string[] Inserts { get; set; } = Array.Empty<string>();

	[JsonPropertyName("authors")]
	public string[] Authors { get; set; } = Array.Empty<string>();

	[JsonPropertyName("illustrators")]
	public string[] Illustrators { get; set; } = Array.Empty<string>();

	[JsonPropertyName("editors")]
	public string[] Editors { get; set; } = Array.Empty<string>();

	[JsonPropertyName("translators")]
	public string[] Translators { get; set; } = Array.Empty<string>();
}
