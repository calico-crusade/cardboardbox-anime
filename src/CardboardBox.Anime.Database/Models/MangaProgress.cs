namespace CardboardBox.Anime.Database
{
	public class MangaProgress
	{
		public DbManga Manga { get; set; }
		public DbMangaProgress Progress { get; set; }
		public DbMangaChapter Chapter { get; set; }
		public MangaStats Stats { get; set; }

		public MangaProgress(
			DbManga manga, 
			DbMangaProgress progress, 
			DbMangaChapter chapter, 
			MangaStats stats)
		{
			Manga = manga;
			Progress = progress;
			Chapter = chapter;
			Stats = stats;
		}
	}

	public class MangaStats
	{
		[JsonPropertyName("maxChapterNum")]
		public int MaxChapterNum { get; set; }

		[JsonPropertyName("chapterNum")]
		public int ChapterNum { get; set; }

		[JsonPropertyName("pageCount")]
		public int PageCount { get; set; }

		[JsonPropertyName("chapterProgress")]
		public double ChapterProgress { get; set; }

		[JsonPropertyName("pageProgress")]
		public double PageProgress { get; set; }
	}
}
