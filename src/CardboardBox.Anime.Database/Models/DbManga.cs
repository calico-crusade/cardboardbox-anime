namespace CardboardBox.Anime.Database
{
	public class DbManga : DbObject
	{
		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("sourceId")]
		public string SourceId { get; set; } = string.Empty;

		[JsonPropertyName("provider")]
		public string Provider { get; set; } = string.Empty;

		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;

		[JsonPropertyName("cover")]
		public string Cover { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;

		[JsonPropertyName("altTitles")]
		public string[] AltTitles { get; set; } = Array.Empty<string>();

		[JsonPropertyName("tags")]
		public string[] Tags { get; set; } = Array.Empty<string>();
	}
}
