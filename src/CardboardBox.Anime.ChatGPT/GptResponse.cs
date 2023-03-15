namespace CardboardBox.Anime.ChatGPT;

public class GptResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("object")]
	public string Object { get; set; } = string.Empty;

	[JsonPropertyName("created")]
	public long Created { get; set; }

	[JsonPropertyName("choices")]
	public GptChoice[] Choices { get; set; } = Array.Empty<GptChoice>();

	[JsonPropertyName("usage")]
	public GptUsage Usage { get; set; } = new();

	public class GptChoice
	{
		[JsonPropertyName("index")]
		public int Index { get; set; }

		[JsonPropertyName("message")]
		public GptMessage Message { get; set; } = new();

		[JsonPropertyName("finish_reason")]
		public string FinishReason { get; set; } = string.Empty;
	}

	public class GptUsage
	{
		[JsonPropertyName("prompt_tokens")]
		public int PromptTokens { get; set; }

		[JsonPropertyName("completion_tokens")]
		public int CompletionTokens { get; set; }

		[JsonPropertyName("total_tokens")]
		public int TotalTokens { get; set; }
	}
}

