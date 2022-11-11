namespace CardboardBox.Manga
{
	using Anime.Database;

	public class MangaWithChapters
	{
		[JsonPropertyName("manga")]
		public DbManga Manga { get; set; } = new();

		[JsonPropertyName("chapters")]
		public DbMangaChapter[] Chapters { get; set; } = Array.Empty<DbMangaChapter>();

		public MangaWithChapters() { }

		public MangaWithChapters(DbManga manga, DbMangaChapter[] chapters)
		{
			Manga = manga;
			Chapters = chapters;
		}

		public void Deconstruct(out DbManga manga, out DbMangaChapter[] chapters)
		{
			manga = Manga;
			chapters = Chapters;
		}
	}
}
