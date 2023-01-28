namespace CardboardBox.Manga;

public interface IMangaSource
{
	string HomeUrl { get; }
	string Provider { get; }

	(bool matches, string? part) MatchesProvider(string url);

	Task<Manga?> Manga(string id);

	Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId);
}

public interface IMangaUrlSource : IMangaSource
{
	Task<MangaChapterPages?> ChapterPages(string url);
}
