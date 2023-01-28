namespace CardboardBox.LightNovel.Core;

using Anime.Database;

public abstract class HashBase : DbObject
{
	[JsonPropertyName("hashId")]
	public string HashId { get; set; } = string.Empty;

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;
}

public abstract class BookBase : HashBase
{
	[JsonPropertyName("ordinal")]
	public long Ordinal { get; set; }
}
