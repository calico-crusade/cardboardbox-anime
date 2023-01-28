namespace CardboardBox.Anime.Core.Models;

[BsonIgnoreExtraElements]
public class Image
{
	[JsonPropertyName("width")]
	[BsonElement("width")]
	public int? Width { get; set; }

	[JsonPropertyName("height")]
	[BsonElement("height")]
	public int? Height { get; set; }

	[JsonPropertyName("type")]
	[BsonElement("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("source")]
	[BsonElement("source")]
	public string Source { get; set; } = "";

	[JsonPropertyName("platformId")]
	[BsonElement("platform_id")]
	public string PlatformId { get; set; } = "";
}
