namespace CardboardBox.Anime.ChatGPT;

public class GptChat
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = "gpt-3.5-turbo-0301";

	[JsonPropertyName("messages")]
	public List<GptMessage> Messages { get; set; } = new();
}
