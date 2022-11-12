namespace CardboardBox.Anime.Database
{
	public class DbMangaFavourite : DbObject
	{
		[JsonPropertyName("profileId")]
		public long ProfileId { get; set; }

		[JsonPropertyName("mangaId")]
		public long MangaId { get; set; }
	}
}
