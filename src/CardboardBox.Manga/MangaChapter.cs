namespace CardboardBox.Manga
{
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
	}

	public class MangaChapterPages : MangaChapter
	{
		[JsonPropertyName("pages"), BsonElement("pages")]
		public string[] Pages { get; set; } = Array.Empty<string>();
	}
}