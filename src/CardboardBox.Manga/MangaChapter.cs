using MongoDB.Bson.Serialization.Attributes;

namespace CardboardBox.Manga;

public class MangaChapter
{
	[JsonPropertyName("title"), BsonElement("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("url"), BsonElement("url")]
	public string Url { get; set; } = string.Empty;

	[JsonPropertyName("id"), BsonElement("chapter_id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("number"), BsonElement("ordinal")]
	public double Number { get; set; }

	[JsonPropertyName("volume"), BsonElement("volume")]
	public double? Volume { get; set; }

	[JsonPropertyName("externalUrl"), BsonElement("external_url")]
	public string? ExternalUrl { get; set; }

	[JsonPropertyName("attributes"), BsonElement("attributes")]
	public List<MangaAttribute> Attributes { get; set; } = new();
}

public class MangaChapterPages : MangaChapter
{
	[JsonPropertyName("pages"), BsonElement("pages")]
	public string[] Pages { get; set; } = Array.Empty<string>();
}