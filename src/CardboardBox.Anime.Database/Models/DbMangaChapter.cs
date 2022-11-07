namespace CardboardBox.Anime.Database
{
	public class DbMangaChapter : DbObject
	{
		[JsonPropertyName("mangaId")]
		public long MangaId { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;

		[JsonPropertyName("sourceId")]
		public string SourceId { get; set; } = string.Empty;

		[JsonPropertyName("ordinal")]
		public double Ordinal { get; set; }

		[JsonPropertyName("language")]
		public string Language { get; set; } = "en";

		[JsonPropertyName("pages")]
		public string[] Pages { get; set; } = Array.Empty<string>();
	}
}
