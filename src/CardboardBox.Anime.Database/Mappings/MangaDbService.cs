using Dapper;

namespace CardboardBox.Anime.Database
{
	using Core;
	using Core.Models;
	using Generation;

	public interface IMangaDbService
	{
		Task<long> Upsert(DbManga manga);

		Task<long> Upsert(DbMangaChapter chapter);

		Task<long> Upsert(DbMangaProgress progress);

		Task<DbManga?> Get(string sourceId); 

		Task<DbManga?> Get(long id);

		Task<DbManga?> GetByHashId(string hashId);

		Task<DbMangaChapter[]> Chapters(long mangaId, string language = "en");

		Task<PaginatedResult<DbManga>> Paginate(int page = 1, int size = 100);

		Task<DbMangaChapter?> GetChapter(long id);

		Task<DbMangaProgress?> GetProgress(string platformId, long mangaId);

		Task<DbMangaProgress?> GetProgress(string platformId, string mangaId);

		Task<Filter[]> Filters();

		Task<PaginatedResult<MangaProgress>> Search(MangaFilter filter, string? platformId);

		Task SetPages(long id, string[] pages);

		Task<bool?> Favourite(string platformId, long mangaId);

		Task<DbMangaBookmark[]> Bookmarks(long id, string? platformId);

		Task<bool> IsFavourite(string? platformId, long mangaId);

		Task<MangaWithChapters?> GetManga(long id, string? platformId);
		Task<MangaWithChapters?> GetManga(string id, string? platformId);

		Task Bookmark(long id, long chapterId, int[] pages, string platformId);

		Task<DbManga[]> FirstUpdated(int count);

		Task<MangaWithChapters?> Random(string? platformId);

		Task<PaginatedResult<MangaProgress>> Touched(string? platformId, int page, int size, TouchedState state = TouchedState.All);

		Task<PaginatedResult<MangaProgress>> Since(string? platform, DateTime since, int page, int size);
	}

	public class MangaDbService : OrmMapExtended<DbManga>, IMangaDbService
	{
		private const string TABLE_NAME_MANGA = "manga";
		private const string TABLE_NAME_MANGA_CHAPTER = "manga_chapter";
		private const string TABLE_NAME_MANGA_PROGRESS = "manga_progress";
		private const string TABLE_NAME_MANGA_FAVOURITES = "manga_favourites";
		private const string TABLE_NAME_MANGA_BOOKMARKS = "manga_bookmarks";

		private string? _upsertMangaQuery;
		private string? _upsertMangaChapterQuery;
		private string? _upsertMangaProgressQuery;
		private string? _upsertMangaBookmarkQuery;
		private string? _getChapterQuery;
		private string? _getMangaQuery;

		public override string TableName => TABLE_NAME_MANGA;

		private readonly IProfileDbService _prof;

		public MangaDbService(
			IDbQueryBuilderService query, 
			ISqlService sql, 
			IProfileDbService prof) : base(query, sql) 
		{ 
			_prof = prof;
		}

		public MangaSortField[] SortFields()
		{
			return new[]
			{
				new MangaSortField("Title", 0, "m.title"),
				new("Provider", 1, "m.provider"),
				new("Latest Chapter", 2, "t.latest_chapter"),
				new("Description", 3, "m.description"),
				new("Updated", 4, "m.updated_at"),
				new("Created", 5, "m.created_at")
			};
		}

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
				v => v.With(t => t.Id).With(t => t.CreatedAt).With(t => t.Pages),
				v => v.Id);

			return _sql.ExecuteScalar<long>(_upsertMangaChapterQuery, chapter);
		}

		public Task SetPages(long id, string[] pages)
		{
			const string QUERY = "UPDATE manga_chapter SET pages = :pages WHERE id = :id";
			return _sql.Execute(QUERY, new { id, pages });
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

		public Task<DbManga?> GetByHashId(string hashId)
		{
			const string QUERY = @"SELECT * FROM manga WHERE hash_id = :hashId AND deleted_at IS NULL";
			return _sql.Fetch<DbManga?>(QUERY, new { hashId });
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

		public Task<DbMangaProgress?> GetProgress(string platformId, string mangaId)
		{
			const string QUERY = @"SELECT 
	mp.*
FROM manga_progress mp
JOIN manga m ON mp.manga_id = m.id
JOIN profiles p ON p.id = mp.profile_id
WHERE
	p.platform_id = :platformId AND
	m.hash_id = :mangaId AND
	mp.deleted_at IS NULL AND
	p.deleted_at IS NULL";
			return _sql.Fetch<DbMangaProgress?>(QUERY, new { platformId, mangaId });
		}

		public async Task<Filter[]> Filters()
		{
			const string QUERY = @"SELECT
    *
FROM (
    SELECT 
        DISTINCT 
        'tag' as key,
        unnest(tags) as value
    FROM manga) x 
ORDER BY key, value";
			var filters = await _sql.Get<DbFilter>(QUERY);
			return filters
				.GroupBy(t => t.Key, t => t.Value)
				.Select(t => new Filter(t.Key, t.ToArray()))
				.Append(new Filter("sorts", SortFields().Select(t => t.Name).ToArray()))
				.ToArray();
		}

		public async Task<PaginatedResult<MangaProgress>> Search(MangaFilter filter, string? platformId)
		{
			const string QUERY = @"CREATE TEMP TABLE touched_manga AS
SELECT
    t.*
FROM get_manga(:platformId, :state) t
JOIN manga m ON m.id = t.manga_id
WHERE
    {0};

SELECT
    m.*,
    '' as split,
    mp.*,
    '' as split,
    mc.*,
    '' as split,
    t.*
FROM touched_manga t
JOIN manga m ON m.id = t.manga_id
JOIN manga_chapter mc ON mc.id = t.manga_chapter_id
LEFT JOIN manga_progress mp ON mp.id = t.progress_id
ORDER BY {2} {1}
LIMIT :size OFFSET :offset;

SELECT COUNT(*) FROM touched_manga;
DROP TABLE touched_manga;";

			var sortField = SortFields().FirstOrDefault(t => t.Id == (filter.Sort ?? 0))?.SqlName ?? "m.title";

			var parts = new List<string>();
			var pars = new DynamicParameters();
			pars.Add("offset", (filter.Page - 1) * filter.Size);
			pars.Add("size", filter.Size);
			pars.Add("platformId", platformId);
			pars.Add("state", (int)filter.State);

			if (!string.IsNullOrEmpty(filter.Search))
			{
				parts.Add("m.fts @@ phraseto_tsquery('english', :search)");
				pars.Add("search", filter.Search);
			}

			if (filter.Include != null && filter.Include.Length > 0)
			{
				parts.Add("m.tags @> :include");
				pars.Add("include", filter.Include);
			}

			if (filter.Exclude != null && filter.Exclude.Length > 0)
			{
				parts.Add("NOT (m.tags && :exclude )");
				pars.Add("exclude", filter.Exclude);
			}

			parts.Add("m.deleted_at IS NULL");
			var where = string.Join(" AND ", parts);
			var sort = filter.Ascending ? "ASC" : "DESC";

			var query = string.Format(QUERY, where, sort, sortField);

			var offset = (filter.Page - 1) * filter.Size;
			using var con = _sql.CreateConnection();
			using var rdr = await con.QueryMultipleAsync(query, pars);

			var results = rdr.Read<DbManga, DbMangaProgress, DbMangaChapter, MangaStats, MangaProgress>((m, p, c, s) => new MangaProgress(m, p, c, s), splitOn: "split");
			var total = await rdr.ReadSingleAsync<int>();
			var pages = (long)Math.Ceiling((double)total / filter.Size);
			return new PaginatedResult<MangaProgress>(pages, total, results.ToArray());
		}

		public async Task<DbMangaBookmark[]> Bookmarks(long id, string? platformId)
		{
			if (string.IsNullOrEmpty(platformId)) return Array.Empty<DbMangaBookmark>();

			const string QUERY = @"SELECT mb.* FROM manga_bookmarks mb
JOIN profiles p ON p.id = mb.profile_id
WHERE p.platform_id = :platformId AND mb.manga_id = :id";
			return (await _sql.Get<DbMangaBookmark>(QUERY, new { id, platformId })) ?? Array.Empty<DbMangaBookmark>();
		}

		public async Task Bookmark(long id, long chapterId, int[] pages, string platformId)
		{
			const string DELETE_QUERY = @"
DELETE FROM manga_bookmarks 
WHERE id IN (
	SELECT
		mb.id
	FROM manga_bookmarks mb 
	JOIN profiles p ON p.id = mb.profile_id
	WHERE p.platform_id = :platformId AND
		  mb.manga_id = :id AND
		  mb.manga_chapter_id = :chapterId
)";
			_upsertMangaBookmarkQuery ??= _query.Upsert<DbMangaBookmark>(TABLE_NAME_MANGA_BOOKMARKS,
				v => v.With(t => t.ProfileId).With(t => t.MangaId).With(t => t.MangaChapterId),
				v => v.With(t => t.Id),
				v => v.With(t => t.Id).With(t => t.CreatedAt));
			if (pages.Length == 0)
			{
				await _sql.Execute(DELETE_QUERY, new { id, chapterId, pages, platformId });
				return;
			}

			var pid = await _prof.Fetch(platformId);
			if (pid == null) return;

			await _sql.Execute(_upsertMangaBookmarkQuery, new DbMangaBookmark
			{
				ProfileId = pid.Id,
				MangaId = id,
				MangaChapterId = chapterId,
				Pages = pages
			});
		}

		public async Task<bool> IsFavourite(string? platformId, long mangaId)
		{
			if (string.IsNullOrEmpty(platformId)) return false;
			const string QUERY = @"SELECT 1 FROM manga_favourites mf 
JOIN profiles p ON p.id = mf.profile_id
WHERE p.platform_id = :platformId AND mf.manga_id = :mangaId";

			var res = await _sql.Fetch<bool?>(QUERY, new { platformId, mangaId });
			return res ?? false;
		}

		public async Task<bool?> Favourite(string platformId, long mangaId)
		{
			const string QUERY = @"SELECT toggle_favourite(:platformId, :mangaId)";
			var res = await _sql.ExecuteScalar<int>(QUERY, new { platformId, mangaId });
			if (res == -1) return null;
			return res == 1;
		}

		public async Task<MangaWithChapters?> GetManga(long id, string? platformId)
		{
			const string QUERY = "SELECT * FROM manga WHERE id = :id;" +
				"SELECT * FROM manga_chapter WHERE manga_id = :id ORDER BY ordinal;";
			const string TARGETED_QUERY = @"SELECT mb.* 
FROM manga_bookmarks mb
JOIN profiles p ON p.id = mb.profile_id
WHERE p.platform_id = :platformId AND mb.manga_id = :id
ORDER BY mb.manga_chapter_id;

SELECT 1 
FROM manga_favourites mf 
JOIN profiles p ON p.id = mf.profile_id
WHERE p.platform_id = :platformId AND mf.manga_id = :id";

			var query = string.IsNullOrEmpty(platformId) ? QUERY : QUERY + TARGETED_QUERY;

			using var con = _sql.CreateConnection();
			using var rdr = await con.QueryMultipleAsync(query, new { id, platformId });

			var manga = await rdr.ReadFirstOrDefaultAsync<DbManga>();
			if (manga == null) return null;

			var chapters = await rdr.ReadAsync<DbMangaChapter>();
			if (string.IsNullOrEmpty(platformId))
				return new(manga, chapters.ToArray());

			var bookmarks = await rdr.ReadAsync<DbMangaBookmark>();
			var favourite = (await rdr.ReadSingleOrDefaultAsync<bool?>()) ?? false;

			return new(manga, chapters.ToArray(), bookmarks.ToArray(), favourite);
		}

		public async Task<MangaWithChapters?> GetManga(string id, string? platformId)
		{
			const string QUERY = "SELECT * FROM manga WHERE hash_id = :id;" +
				@"SELECT c.* FROM manga_chapter c
JOIN manga m ON m.id = c.manga_id
WHERE m.hash_id = :id ORDER BY c.ordinal;";
			const string TARGETED_QUERY = @"SELECT mb.* 
FROM manga_bookmarks mb
JOIN manga m ON m.id = mb.manga_id
JOIN profiles p ON p.id = mb.profile_id
WHERE p.platform_id = :platformId AND m.hash_id = :id
ORDER BY mb.manga_chapter_id;

SELECT 1 
FROM manga_favourites mf 
JOIN manga m ON m.id = mf.manga_id
JOIN profiles p ON p.id = mf.profile_id
WHERE p.platform_id = :platformId AND m.hash_id = :id";

			var query = string.IsNullOrEmpty(platformId) ? QUERY : QUERY + TARGETED_QUERY;

			using var con = _sql.CreateConnection();
			using var rdr = await con.QueryMultipleAsync(query, new { id, platformId });

			var manga = await rdr.ReadFirstOrDefaultAsync<DbManga>();
			if (manga == null) return null;

			var chapters = await rdr.ReadAsync<DbMangaChapter>();
			if (string.IsNullOrEmpty(platformId))
				return new(manga, chapters.ToArray());

			var bookmarks = await rdr.ReadAsync<DbMangaBookmark>();
			var favourite = (await rdr.ReadSingleOrDefaultAsync<bool?>()) ?? false;

			return new(manga, chapters.ToArray(), bookmarks.ToArray(), favourite);
		}

		public Task<DbManga[]> FirstUpdated(int count)
		{
			const string QUERY = "SELECT * FROM manga ORDER BY updated_at ASC LIMIT :count";
			return _sql.Get<DbManga>(QUERY, new { count }); 
		}

		public async Task<MangaWithChapters?> Random(string? platformId)
		{
			const string RANDOM_QUERY = "SELECT * FROM manga ORDER BY random() LIMIT 1;";
			const string QUERY = "SELECT * FROM manga_chapter WHERE manga_id = :id ORDER BY ordinal;";
			const string TARGETED_QUERY = @"SELECT mb.* 
FROM manga_bookmarks mb
JOIN profiles p ON p.id = mb.profile_id
WHERE p.platform_id = :platformId AND mb.manga_id = :id
ORDER BY mb.manga_chapter_id;

SELECT 1 
FROM manga_favourites mf 
JOIN profiles p ON p.id = mf.profile_id
WHERE p.platform_id = :platformId AND mf.manga_id = :id";

			using var con = _sql.CreateConnection();
			var manga = await con.QueryFirstOrDefaultAsync<DbManga>(RANDOM_QUERY);
			if (manga == null) return null;

			var query = string.IsNullOrEmpty(platformId) ? QUERY : QUERY + TARGETED_QUERY;

			using var rdr = await con.QueryMultipleAsync(query, new { id = manga.Id, platformId });

			var chapters = await rdr.ReadAsync<DbMangaChapter>();
			if (string.IsNullOrEmpty(platformId))
				return new(manga, chapters.ToArray());

			var bookmarks = await rdr.ReadAsync<DbMangaBookmark>();
			var favourite = (await rdr.ReadSingleOrDefaultAsync<bool?>()) ?? false;

			return new(manga, chapters.ToArray(), bookmarks.ToArray(), favourite);
		}

		public async Task<PaginatedResult<MangaProgress>> Touched(string? platformId, int page, int size, TouchedState state = TouchedState.All)
		{
			const string QUERY = @"CREATE TEMP TABLE touched_manga AS
SELECT
    *
FROM get_manga(:platformId, :state);

SELECT
    m.*,
    '' as split,
    mp.*,
    '' as split,
    mc.*,
    '' as split,
    t.*
FROM touched_manga t
JOIN manga m ON m.id = t.manga_id
JOIN manga_chapter mc ON mc.id = t.manga_chapter_id
LEFT JOIN manga_progress mp ON mp.id = t.progress_id
ORDER BY m.title ASC
LIMIT :size OFFSET :offset;

SELECT COUNT(*) FROM touched_manga;

DROP TABLE touched_manga;";

			var offset = (page - 1) * size;
			using var con = _sql.CreateConnection();
			using var rdr = await con.QueryMultipleAsync(QUERY, new { platformId, state = (int)state, offset, size });

			var results = rdr.Read<DbManga, DbMangaProgress, DbMangaChapter, MangaStats, MangaProgress>((m, p, c, s) =>  new MangaProgress(m, p, c, s), splitOn: "split");
			var total = await rdr.ReadSingleAsync<int>();
			var pages = (long)Math.Ceiling((double)total / size);
			return new PaginatedResult<MangaProgress>(pages, total, results.ToArray());
		}

		public async Task<PaginatedResult<MangaProgress>> Since(string? platformId, DateTime since, int page, int size)
		{
			const string QUERY = @"CREATE TEMP TABLE touched_manga AS
SELECT
    t.*
FROM get_manga(:platformId, :state) t
WHERE 
    t.latest_chapter >= :since;

SELECT
    m.*,
    '' as split,
    mp.*,
    '' as split,
    mc.*,
    '' as split,
    t.*
FROM touched_manga t
JOIN manga m ON m.id = t.manga_id
JOIN manga_chapter mc ON mc.id = t.manga_chapter_id
LEFT JOIN manga_progress mp ON mp.id = t.progress_id
ORDER BY t.latest_chapter DESC
LIMIT :size OFFSET :offset;

SELECT COUNT(*) FROM touched_manga;

DROP TABLE touched_manga;";

			var offset = (page - 1) * size;
			using var con = _sql.CreateConnection();
			using var rdr = await con.QueryMultipleAsync(QUERY, new { platformId, state = (int)TouchedState.InProgress, offset, size, since });

			var results = rdr.Read<DbManga, DbMangaProgress, DbMangaChapter, MangaStats, MangaProgress>((m, p, c, s) => new MangaProgress(m, p, c, s), splitOn: "split");
			var total = await rdr.ReadSingleAsync<int>();
			var pages = (long)Math.Ceiling((double)total / size);
			return new PaginatedResult<MangaProgress>(pages, total, results.ToArray());
		}
	}

	public record class MangaSortField(string Name, int Id, string SqlName);
}
