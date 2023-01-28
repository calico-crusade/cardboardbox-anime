namespace CardboardBox.Anime.Core.Models;

[BsonIgnoreExtraElements]
public class Metadata
{
	[JsonPropertyName("languages")]
	[BsonElement("languages")]
	public List<string> Languages { get; set; } = new();

	[JsonPropertyName("languageTypes")]
	[BsonElement("language_types")]
	public List<string> LanguageTypes { get; set; } = new();

	[JsonPropertyName("ratings")]
	[BsonElement("ratings")]
	public List<string> Ratings { get; set; } = new();

	[JsonPropertyName("tags")]
	[BsonElement("tags")]
	public List<string> Tags { get; set; } = new();

	[JsonPropertyName("mature")]
	[BsonElement("mature")]
	public bool Mature { get; set; }

	[JsonPropertyName("seasons")]
	[BsonElement("seasons")]
	public List<Season> Seasons { get; set; } = new();

	[JsonPropertyName("ext")]
	[BsonElement("ext")]
	public Dictionary<string, string> Ext { get; set; } = new();
}
