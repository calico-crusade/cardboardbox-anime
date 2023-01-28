namespace CardboardBox.LightNovel.Core;

public class PartialScaffold
{
	[JsonPropertyName("series")]
	public Series Series { get; set; } = new();

	[JsonPropertyName("books")]
	public PartialBookScaffold[] Books { get; set; } = Array.Empty<PartialBookScaffold>();
}

public class PartialBookScaffold
{
	[JsonPropertyName("book")]
	public Book Book { get; set; } = new();

	[JsonPropertyName("chapters")]
	public Chapter[] Chapters { get; set; } = Array.Empty<Chapter>();
}
