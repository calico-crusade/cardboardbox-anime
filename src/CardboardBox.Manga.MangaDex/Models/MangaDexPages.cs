namespace CardboardBox.Manga.MangaDex.Models;

public class MangaDexPages
{
	[JsonPropertyName("result")]
	public string Result { get; set; } = string.Empty;

	[JsonPropertyName("baseUrl")]
	public string BaseUrl { get; set; } = string.Empty;

	[JsonPropertyName("chapter")]
	public MangaDexChapter Chapter { get; set; } = new();

	public string[] Images => GenerateImageLinks();

	public string[] GenerateImageLinks()
	{
		return Chapter
			.Data
			.Select(t => $"{BaseUrl}/data/{Chapter.Hash}/{t}")
			.ToArray();
	}

	public class MangaDexChapter
	{
		[JsonPropertyName("hash")]
		public string Hash { get; set; } = string.Empty;

		[JsonPropertyName("data")]
		public string[] Data { get; set; } = Array.Empty<string>();

		[JsonPropertyName("dataSaver")]
		public string[] DataSaver { get; set; } = Array.Empty<string>();
	}
}
