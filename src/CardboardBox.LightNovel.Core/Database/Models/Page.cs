namespace CardboardBox.LightNovel.Core
{
	public class Page : BookBase
	{
		[JsonPropertyName("seriesId")]
		public long SeriesId { get; set; }

		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;

		[JsonPropertyName("nextUrl")]
		public string? NextUrl { get; set; }

		[JsonPropertyName("content")]
		public string Content { get; set; } = string.Empty;

		[JsonPropertyName("mimetype")]
		public string Mimetype { get; set; } = string.Empty;
	}
}
