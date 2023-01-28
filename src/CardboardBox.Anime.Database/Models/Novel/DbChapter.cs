namespace CardboardBox.Anime.Database;

public class DbChapter : DbObject
{
	[JsonPropertyName("hashId")]
	public string HashId { get; set; } = string.Empty;

	[JsonPropertyName("bookId")]
	public string BookId { get; set; } = string.Empty;

	[JsonPropertyName("book")]
	public string Book { get; set; } = string.Empty;

	[JsonPropertyName("chapter")]
	public string Chapter { get; set; } = string.Empty;

	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;

	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty;

	[JsonPropertyName("nextUrl")]
	public string NextUrl { get; set; } = string.Empty;

	[JsonPropertyName("ordinal")]
	public int Ordinal { get; set; }
}

public class DbBook
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("chapters")]
	public int Chapters { get; set; }

	[JsonPropertyName("updatedAt")]
	public DateTime UpdatedAt { get; set; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("lastChapterUrl")]
	public string LastChapterUrl { get; set; } = string.Empty;

	[JsonPropertyName("lastChapterId")]
	public int LastChapterId { get; set; }

	[JsonPropertyName("lastChapterOrdinal")]
	public int LastChapterOrdinal { get; set; }
}

public class DbChapterLimited : DbObject
{
	[JsonPropertyName("hashId")]
	public string HashId { get; set; } = string.Empty;

	[JsonPropertyName("chapter")]
	public string Chapter { get; set; } = string.Empty;

	[JsonPropertyName("ordinal")]
	public int Ordinal { get; set; }
}
