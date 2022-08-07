namespace CardboardBox.Anime.Database
{
	public class DbAnime : DbObject
	{
		[JsonPropertyName("hashId")]
		public string HashId { get; set; } = "";

		[JsonPropertyName("animeId")]
		public string AnimeId { get; set; } = "";

		[JsonPropertyName("link")]
		public string Link { get; set; } = "";

		[JsonPropertyName("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("description")]
		public string Description { get; set; } = "";

		[JsonPropertyName("platformId")]
		public string PlatformId { get; set; } = "";

		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("mature")]
		public bool Mature { get; set; } = false;

		[JsonPropertyName("languages")]
		public string[] Languages { get; set; } = Array.Empty<string>();

		[JsonPropertyName("languageTypes")]
		public string[] LanguageTypes { get; set; } = Array.Empty<string>();

		[JsonPropertyName("ratings")]
		public string[] Ratings { get; set; } = Array.Empty<string>();

		[JsonPropertyName("tags")]
		public string[] Tags { get; set; } = Array.Empty<string>();

		[JsonPropertyName("images")]
		public DbImage[] Images { get; set; } = Array.Empty<DbImage>();
	}
}