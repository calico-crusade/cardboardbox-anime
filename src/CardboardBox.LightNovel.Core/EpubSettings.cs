namespace CardboardBox.LightNovel.Core
{
	public class EpubSettings
	{
		[JsonPropertyName("stylesheetUrl")]
		public string? StylesheetUrl { get; set; }

		[JsonPropertyName("coverUrl")]
		public string? CoverUrl { get; set; }

		[JsonPropertyName("start")]
		public int Start { get; set; }

		[JsonPropertyName("stop")]
		public int Stop { get; set; }

		[JsonPropertyName("vol")]
		public int Vol { get; set; }

		[JsonPropertyName("forwardUrls")]
		public string[] ForwardUrls { get; set; } = Array.Empty<string>();

		[JsonPropertyName("insertUrls")]
		public string[] InsertUrls { get; set; } = Array.Empty<string>();

		[JsonPropertyName("author")]
		public string? Author { get; set; }

		[JsonPropertyName("editor")]
		public string? Editor { get; set; }

		[JsonPropertyName("publisher")]
		public string? Publisher { get; set; }

		[JsonPropertyName("translator")]
		public string? Translator { get; set; }

		[JsonPropertyName("illustrator")]
		public string? Illustrator { get; set; }

		[JsonPropertyName("skipGeneration")]
		public bool SkipGeneration { get; set; } = false;
	}
}
