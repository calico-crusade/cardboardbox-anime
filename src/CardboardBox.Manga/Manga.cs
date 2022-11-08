namespace CardboardBox.Manga
{
	public class Manga
	{
		[JsonPropertyName("title"), BsonElement("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("id"), BsonElement("manga_id")]
		public string Id { get; set; } = string.Empty;

		[JsonPropertyName("provider"), BsonElement("provider")]
		public string Provider { get; set; } = string.Empty;

		[JsonPropertyName("homePage"), BsonElement("home_page")]
		public string HomePage { get; set; } = string.Empty;

		[JsonPropertyName("cover"), BsonElement("cover")]
		public string Cover { get; set; } = string.Empty;

		[JsonPropertyName("description"), BsonElement("description")]
		public string Description { get; set; } = string.Empty;

		[JsonPropertyName("altTitles"), BsonElement("alt_titles")]
		public string[] AltTitles { get; set; } = Array.Empty<string>();

		[JsonPropertyName("tags"), BsonElement("tags")]
		public string[] Tags { get; set; } = Array.Empty<string>();

		[JsonPropertyName("chapters"), BsonElement("chapters")]
		public List<MangaChapter> Chapters { get; set; } = new();
	}
}
