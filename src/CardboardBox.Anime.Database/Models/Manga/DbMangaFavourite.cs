namespace CardboardBox.Anime.Database;

public class DbMangaFavourite : DbObjectInt
{
	[JsonPropertyName("profileId")]
	public long ProfileId { get; set; }

	[JsonPropertyName("mangaId")]
	public long MangaId { get; set; }
}
