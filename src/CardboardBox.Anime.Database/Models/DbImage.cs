namespace CardboardBox.Anime.Database
{
	public class DbImage
	{
		[JsonPropertyName("width")]
		public int? Width { get; set; }

		[JsonPropertyName("height")]
		public int? Height { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("source")]
		public string Source { get; set; } = "";

		[JsonPropertyName("platformId")]
		public string PlatformId { get; set; } = "";
	}
}
