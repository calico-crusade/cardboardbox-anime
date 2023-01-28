namespace CardboardBox.Anime.Database;

public class MangaProgress
{
	[JsonPropertyName("manga")]
	public DbManga Manga { get; set; } = new();

	[JsonPropertyName("progress")]
	public DbMangaProgress? Progress { get; set; }

	[JsonPropertyName("chapter")]
	public DbMangaChapter Chapter { get; set; } = new();

	[JsonPropertyName("stats")]
	public MangaStats Stats { get; set; } = new();

	public MangaProgress() { }

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

	[JsonPropertyName("latestChapter")]
	public DateTime? LatestChapter { get; set; }

	[JsonPropertyName("completed")]
	public bool Completed { get; set; } = false;

	[JsonPropertyName("firstChapterId")]
	public long FirstChapterId { get; set; }

	[JsonPropertyName("progressChapterId")]
	public long? ProgressChapterId { get; set; }

	[JsonPropertyName("progressId")]
	public long? ProgressId { get; set; }
}
