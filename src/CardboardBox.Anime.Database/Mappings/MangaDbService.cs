namespace CardboardBox.Anime.Database
{
	using Generation;

	public interface IMangaDbService
	{
		Task<long> Upsert(DbManga manga);

		Task<long> Upsert(DbMangaChapter chapter);

		Task<long> Upsert(DbMangaProgress progress);

		Task<DbManga?> Get(string sourceId); 

		Task<DbManga?> Get(long id);

		Task<DbMangaChapter[]> Chapters(long mangaId, string language = "en");

		Task<PaginatedResult<DbManga>> Paginate(int page = 1, int size = 100);

		Task<DbMangaChapter?> GetChapter(long id);

		Task<DbMangaProgress?> GetProgress(string platformId, long mangaId);
	}

	public class MangaDbService : OrmMapExtended<DbManga>, IMangaDbService
	{
		private const string TABLE_NAME_MANGA = "manga";
		private const string TABLE_NAME_MANGA_CHAPTER = "manga_chapter";
		private const string TABLE_NAME_MANGA_PROGRESS = "manga_progress";

		private string? _upsertMangaQuery;
		private string? _upsertMangaChapterQuery;
		private string? _upsertMangaProgressQuery;
		private string? _getChapterQuery;
		private string? _getMangaQuery;

		public override string TableName => TABLE_NAME_MANGA;

		public MangaDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<long> Upsert(DbManga manga)
		{
			_upsertMangaQuery ??= _query.Upsert<DbManga, long>(TableName,
				(v) => v.With(t => t.Provider).With(t => t.SourceId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertMangaQuery, manga);
		}

		public Task<long> Upsert(DbMangaChapter chapter)
		{
			_upsertMangaChapterQuery ??= _query.Upsert<DbMangaChapter, long>(TABLE_NAME_MANGA_CHAPTER,
				v => v.With(t => t.MangaId).With(t => t.SourceId).With(t => t.Language),
				v => v.With(t => t.Id),
				v => v.With(t => t.Id).With(t => t.CreatedAt),
				v => v.Id);

			return _sql.ExecuteScalar<long>(_upsertMangaChapterQuery, chapter);
		}

		public Task<long> Upsert(DbMangaProgress progress)
		{
			_upsertMangaProgressQuery ??= _query.Upsert<DbMangaProgress, long>(TABLE_NAME_MANGA_PROGRESS,
				v => v.With(t => t.ProfileId).With(t => t.MangaId),
				v => v.With(t => t.Id),
				v => v.With(t => t.Id).With(t => t.CreatedAt),
				v => v.Id);

			return _sql.ExecuteScalar<long>(_upsertMangaProgressQuery, progress);
		}

		public override Task<PaginatedResult<DbManga>> Paginate(int page = 1, int size = 100)
		{
			const string QUERY = "SELECT * FROM manga WHERE deleted_at IS NULL ORDER BY title ASC LIMIT :size OFFSET :offset;" + 
				"SELECT COUNT(*) FROM manga WHERE deleted_at IS NULL;";

			return Paginate(QUERY, null, page, size);
		}

		public Task<DbMangaChapter[]> Chapters(long mangaId, string language = "en")
		{
			const string QUERY = @"SELECT 
	* 
FROM manga_chapter 
WHERE 
	manga_id = :mangaId AND 
	language = :language AND 
	deleted_at IS NULL
ORDER BY ordinal";
			return _sql.Get<DbMangaChapter>(QUERY, new { mangaId, language });
		}

		public Task<DbManga?> Get(string sourceId)
		{
			const string QUERY = @"SELECT * FROM manga WHERE source_id = :sourceId AND deleted_at IS NULL";
			return _sql.Fetch<DbManga?>(QUERY, new { sourceId });
		}

		public Task<DbManga?> Get(long id)
		{
			_getMangaQuery ??= _query.Select<DbManga>(TABLE_NAME_MANGA, t => t.With(a => a.Id));
			return _sql.Fetch<DbManga?>(_getMangaQuery, new { id });
		}

		public Task<DbMangaChapter?> GetChapter(long id)
		{
			_getChapterQuery ??= _query.Select<DbMangaChapter>(TABLE_NAME_MANGA_CHAPTER, t => t.With(a => a.Id));
			return _sql.Fetch<DbMangaChapter?>(_getChapterQuery, new { id });
		}

		public Task<DbMangaProgress?> GetProgress(string platformId, long mangaId)
		{
			const string QUERY = @"SELECT 
	mp.*
FROM manga_progress mp
JOIN profiles p ON p.id = mp.profile_id
WHERE
	p.platform_id = :platformId AND
	mp.manga_id = :mangaId AND
	mp.deleted_at IS NULL AND
	p.deleted_at IS NULL";
			return _sql.Fetch<DbMangaProgress?>(QUERY, new { platformId, mangaId });
		}
	}
}
