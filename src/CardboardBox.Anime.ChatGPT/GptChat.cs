namespace CardboardBox.Anime.ChatGPT;

public class GptChat
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = "gpt-4-turbo";

	[JsonPropertyName("messages")]
	public List<GptMessage> Messages { get; set; } = new();
}
