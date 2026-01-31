namespace CardboardBox.Anime.Database;

public class DbMangaProgress : DbObjectInt
{
	[JsonPropertyName("profileId")]
	public long ProfileId { get; set; }

	[JsonPropertyName("mangaId")]
	public long MangaId { get; set; }

	[JsonPropertyName("mangaChapterId")]
	public long? MangaChapterId { get; set; }

	[JsonPropertyName("pageIndex")]
	public int? PageIndex { get; set; }

	[JsonPropertyName("read")]
	public DbMangaChapterProgress[] Read { get; set; } = Array.Empty<DbMangaChapterProgress>();
}
