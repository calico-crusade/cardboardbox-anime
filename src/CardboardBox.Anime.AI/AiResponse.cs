using System.Text.Json.Serialization;

namespace CardboardBox.Anime.AI
{
	public class AiResponse
	{
		[JsonPropertyName("html")]
		public string Html { get; set; } = string.Empty;

		[JsonPropertyName("images")]
		public string[] Images { get; set; } = Array.Empty<string>();

		[JsonPropertyName("info")]
		public string Info { get; set; } = string.Empty;
	}
}
