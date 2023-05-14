using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Api;

public class SettingsRequest
{
	[JsonPropertyName("settings")]
	public string Settings { get; set; } = string.Empty;
}
