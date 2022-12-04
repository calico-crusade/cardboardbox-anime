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
		Task<string[]> MangaPages(DbMangaChapter? chapter, bool refetch);
		Task<MangaWorked[]> Updated(int count, string? platformId);
		Task<long?> ResolveId(string id);
	}

	public class MangaService : IMangaService
	{
		private readonly IMangaSource[] _sources;
		private readonly IMangaDbService _db;

		public MangaService(
			IMangaDbService db,
			IMangakakalotSource mangakakalot, 
			IMangaDexSource mangaDex,
			IMangaClashSource mangaClash,
			INhentaiSource nhentai)
		{
			_db = db;
			_sources = new IMangaSource[]
			{
				mangaDex,
				mangakakalot,
				mangaClash,
				nhentai
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
			return await MangaPages(chapter, refetch);
		}

		public async Task<string[]> MangaPages(DbMangaChapter? chapter, bool refetch)
		{
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

			return await Manga(manga.Id, platformId);
		}

		public Task<MangaWithChapters?> Manga(long id, string? platformId)
		{
			return _db.GetManga(id, platformId);
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
				HashId = GenerateHashId(manga.Provider + " " + manga.Title).ToLower(),
				SourceId = manga.Id,
				Provider = manga.Provider,
				Url = manga.HomePage,
				Cover = manga.Cover,
				Tags = manga.Tags,
				Description = manga.Description,
				AltTitles = manga.AltTitles,
				Nsfw = manga.Nsfw,
				Attributes = manga.Attributes
					.Select(t => new DbMangaAttribute(t.Name, t.Value))
					.ToArray()
			};
			m.Id = await _db.Upsert(m);
			return m;
		}

		public string GenerateHashId(string title)
		{
			return Regex.Replace(title, "[^a-zA-Z0-9 ]", string.Empty).Replace(" ", "-");
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
					Language = language,
					ExternalUrl = chapter.ExternalUrl
				};

				chap.Id = await _db.Upsert(chap);
				yield return chap;
			}
		}

		public async Task<MangaWorked[]> Updated(int count, string? platformId)
		{
			var needs = await _db.FirstUpdated(count);

			return await needs.Select(async t =>
			{
				var (src, id) = DetermineSource(t.Url);
				if (src == null || id == null) return new MangaWorked(t, false);

				var res = await LoadManga(src, id, platformId);
				return new(res?.Manga ?? t, res != null);
			}).WhenAll();
		}

		public async Task<long?> ResolveId(string id)
		{
			if (long.TryParse(id, out var mid))
				return mid;

			return (await _db.GetByHashId(id))?.Id;
		}
	}

	public class MangaWorked
	{
		[JsonPropertyName("manga")]
		public DbManga Manga { get; set; } = new();

		[JsonPropertyName("worked")]
		public bool Worked { get; set; } = false;

		public MangaWorked() { }

		public MangaWorked(DbManga manga, bool worked)
		{
			Manga = manga;
			Worked = worked;
		}
	}
}
