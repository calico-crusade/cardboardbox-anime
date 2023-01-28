namespace CardboardBox.Anime.Database;

public class DbListMap : DbObject
{
	[JsonPropertyName("listId")]
	public long ListId { get; set; }

	[JsonPropertyName("animeId")]
	public long AnimeId { get; set; }
}

public class DbListMapStripped
{
	[JsonPropertyName("listId")]
	public long ListId { get; set; }

	[JsonPropertyName("animeId")]
	public long AnimeId { get; set; }
}

public class DbListMapItem
{
	[JsonPropertyName("listId")]
	public long ListId { get; set; }

	[JsonPropertyName("animeIds")]
	public long[] AnimeIds { get; set; } = Array.Empty<long>();
}
