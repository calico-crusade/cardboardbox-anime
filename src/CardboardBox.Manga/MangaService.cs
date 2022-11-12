namespace CardboardBox.Manga
{
	using Anime.Database;
	using Providers;

	public interface IMangaService
	{
		IMangaSource[] Sources();
		(IMangaSource? source, string? id) DetermineSource(string url);

		Task<PaginatedResult<DbManga>> Manga(int page, int size);
		Task<MangaWithChapters?> Manga(string url, string? platformId, bool forceUpdate = false);
		Task<MangaWithChapters?> Manga(long id, string? platformId);
		Task<string[]> MangaPages(long chapterId, bool refetch);
	}

	public class MangaService : IMangaService
	{
		private readonly IMangaSource[] _sources;
		private readonly IMangaDbService _db;

		public MangaService(
			IMangaDbService db,
			IMangakakalotSource mangakakalot, 
			IMangaDexSource mangaDex)
		{
			_db = db;
			_sources = new IMangaSource[]
			{
				mangaDex,
				mangakakalot
			};
		}

		public IMangaSource[] Sources() => _sources;

		public (IMangaSource? source, string? id) DetermineSource(string url)
		{
			foreach (var source in Sources())
			{
				var (worked, id) = source.MatchesProvider(url);
				if (worked) return (source, id);
			}

			return (null, null);
		}

		public async Task<string[]> MangaPages(long chapterId, bool refetch)
		{
			var chapter = await _db.GetChapter(chapterId);
			if (chapter == null) return Array.Empty<string>();
			if (chapter.Pages.Length > 0 && !refetch) return chapter.Pages;

			var manga = await _db.Get(chapter.MangaId);
			if (manga == null) return Array.Empty<string>();

			var (src, id) = DetermineSource(manga.Url);
			if (src == null || id == null) return Array.Empty<string>();

			var pages = await src.ChapterPages(manga.SourceId, chapter.SourceId);
			if (pages == null) return Array.Empty<string>();

			await _db.SetPages(chapter.Id, pages.Pages);
			chapter.Pages = pages.Pages;
			return chapter.Pages;
		}

		public Task<PaginatedResult<DbManga>> Manga(int page, int size)
		{
			return _db.Paginate(page, size);
		}

		public async Task<MangaWithChapters?> Manga(string url, string? platformId, bool forceUpdate = false)
		{
			var (src, id) = DetermineSource(url);
			if (src == null || id == null) return null;

			var manga = await _db.Get(id);
			if (manga == null || forceUpdate) return await LoadManga(src, id, platformId);

			var chapters = await _db.Chapters(manga.Id);
			var bookmarks = await _db.Bookmarks(manga.Id, platformId);
			var favourite = await _db.IsFavourite(platformId, manga.Id);
			return new(manga, chapters, bookmarks, favourite);
		}

		public async Task<MangaWithChapters?> Manga(long id, string? platformId)
		{
			var manga = await _db.Get(id);
			if (manga == null) return null;

			var chapters = await _db.Chapters(manga.Id);
			var bookmarks = await _db.Bookmarks(manga.Id, platformId);
			var favourite = await _db.IsFavourite(platformId, manga.Id);
			return new(manga, chapters, bookmarks, favourite);
		}

		public async Task<MangaWithChapters?> LoadManga(IMangaSource src, string id, string? platformId)
		{
			var m = await src.Manga(id);
			if (m == null) return null;

			var manga = await ConvertManga(m);
			await ConvertChapters(m, manga.Id).ToArrayAsync();
			return await Manga(manga.Id, platformId);
		}

		public async Task<DbManga> ConvertManga(Manga manga)
		{
			var m = new DbManga
			{
				Title = manga.Title,
				SourceId = manga.Id,
				Provider = manga.Provider,
				Url = manga.HomePage,
				Cover = manga.Cover,
				Tags = manga.Tags,
				Description = manga.Description,
				AltTitles = manga.AltTitles
			};
			m.Id = await _db.Upsert(m);
			return m;
		}

		public async IAsyncEnumerable<DbMangaChapter> ConvertChapters(Manga manga, long id, string language = "en")
		{
			foreach (var chapter in manga.Chapters)
			{
				var chap = new DbMangaChapter
				{
					MangaId = id,
					Title = chapter.Title,
					Url = chapter.Url,
					SourceId = chapter.Id,
					Ordinal = chapter.Number,
					Volume = chapter.Volume,
					Language = language
				};

				chap.Id = await _db.Upsert(chap);
				yield return chap;
			}
		}
	}
}
