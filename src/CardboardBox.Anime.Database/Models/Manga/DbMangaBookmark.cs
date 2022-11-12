namespace CardboardBox.Anime.Database
{
	public class DbMangaBookmark : DbObject
	{
		[JsonPropertyName("profileId")]
		public long ProfileId { get; set; }

		[JsonPropertyName("mangaId")]
		public long MangaId { get; set; }

		[JsonPropertyName("mangaChapterId")]
		public long MangaChapterId { get; set; }

		[JsonPropertyName("pages")]
		public int[] Pages { get; set; } = Array.Empty<int>();
	}
}
