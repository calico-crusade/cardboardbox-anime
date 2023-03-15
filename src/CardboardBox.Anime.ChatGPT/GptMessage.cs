namespace CardboardBox.Anime.ChatGPT;

public class GptMessage
{
	[JsonPropertyName("role")]
	public string Role { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;

	public static GptMessage System(string content) => new() { Role = "system", Content = content };
	public static GptMessage Assistant(string content) => new() { Role = "assistant", Content = content };
	public static GptMessage User(string content) => new() { Role = "user", Content = content };
}
