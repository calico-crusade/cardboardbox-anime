namespace CardboardBox.Anime.Database
{
	public class MangaProgress
	{
		public DbManga Manga { get; set; }
		public DbMangaProgress? Progress { get; set; }
		public DbMangaChapter Chapter { get; set; }
		public MangaStats Stats { get; set; }

		public MangaProgress(
			DbManga manga, 
			DbMangaProgress progress, 
			DbMangaChapter chapter, 
			MangaStats stats)
		{
			Manga = manga;
			Progress = progress?.Id == 0 ? null : progress;
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

		[JsonPropertyName("favourite")]
		public bool Favourite { get; set; } = false;

		[JsonPropertyName("bookmarks")]
		public int[] Bookmarks { get; set; } = Array.Empty<int>();

		[JsonPropertyName("hasBookmarks")]
		public bool HasBookmarks { get; set; } = false;
	}
}
