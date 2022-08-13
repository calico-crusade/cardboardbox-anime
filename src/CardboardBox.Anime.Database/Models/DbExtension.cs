namespace CardboardBox.Anime.Database
{
	public class DbExtension
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("value")]
		public string Value { get; set; } = "";
	}
}
