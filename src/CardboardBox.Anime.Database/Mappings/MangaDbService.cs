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

		Task<DbMangaChapter[]> Chapters(long mangaId, string language = "en");

		Task<PaginatedResult<DbManga>> Paginate(int page = 1, int size = 100);

		Task<DbMangaChapter?> GetChapter(long id);

		Task<DbMangaProgress?> GetProgress(string platformId, long mangaId);

		Task<MangaProgress[]> InProgress(string platformId);

		Task<Filter[]> Filters();

		Task<PaginatedResult<DbManga>> Search(MangaFilter filter);

		Task SetPages(long id, string[] pages);

		Task<bool?> Favourite(string platformId, long mangaId);

		Task<DbMangaBookmark[]> Bookmarks(long id, string? platformId);

		Task<bool> IsFavourite(string? platformId, long mangaId);

		Task<MangaWithChapters?> GetManga(long id, string? platformId);

		Task Bookmark(long id, long chapterId, int[] pages, string platformId);
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

		public Task<DbManga?> Get(long id)
		{
			_getMangaQuery ??= _query.Select<DbManga>(TABLE_NAME_MANGA, t => t.With(a => a.Id));
			return _sql.Fetch<DbManga?>(_getMangaQuery, new { id });
		}

		public async Task<MangaProgress[]> InProgress(string platformId)
		{
			const string QUERY = @"WITH touched_manga AS (
    SELECT DISTINCT m.*, p.id as profile_id FROM manga m
    JOIN manga_bookmarks mb on m.id = mb.manga_id
    JOIN profiles p on mb.profile_id = p.id
    WHERE p.platform_id = '1fc77928-23ea-466e-a7ba-ed9d17e457f7'

    UNION

    SELECT DISTINCT m.*, p.id as profile_id FROM manga m
    JOIN manga_favourites mb on m.id = mb.manga_id
    JOIN profiles p on mb.profile_id = p.id
    WHERE p.platform_id = '1fc77928-23ea-466e-a7ba-ed9d17e457f7'

    UNION

    SELECT DISTINCT m.*, p.id as profile_id FROM manga m
    JOIN manga_progress mb on m.id = mb.manga_id
    JOIN profiles p on mb.profile_id = p.id
    WHERE p.platform_id = '1fc77928-23ea-466e-a7ba-ed9d17e457f7'
), chapter_numbers AS (
    SELECT
        c.*,
        row_number() over (
            PARTITION BY manga_id
            ORDER BY ordinal ASC
        ) as row_num
    FROM manga_chapter c
    JOIN touched_manga m ON m.id = c.manga_id
), max_chapter_numbers AS (
    SELECT
        c.manga_id,
        MAX(c.row_num) as max,
        MIN(c.id) as first_chapter_id
    FROM chapter_numbers c
    GROUP BY c.manga_id
) SELECT DISTINCT
	m.*,
	'' as split,
	mp.*,
	'' as split,
	mc.*,
	'' as split,
	mmc.max as max_chapter_num,
    mc.row_num as chapter_num,
    coalesce(array_length(mc.pages, 1), 0) as page_count,
    (
        CASE
            WHEN mmc.first_chapter_id = mc.id AND mp.page_index IS NULL THEN 0
            ELSE round(mc.row_num / CAST(mmc.max as decimal) * 100, 2)
        END
    ) as chapter_progress,
    coalesce(round(mp.page_index / CAST(array_length(mc.pages, 1) as decimal), 2), 0) * 100 as page_progress,
    coalesce((
        SELECT 1
        FROM manga_favourites mf
        WHERE mf.profile_id = m.profile_id AND mf.manga_id = m.id
    ), 0) as favourite,
    coalesce(mb.pages, '{}') as bookmarks
FROM touched_manga m
LEFT JOIN manga_progress mp ON mp.manga_id = m.id
LEFT JOIN max_chapter_numbers mmc ON mmc.manga_id = m.id
LEFT JOIN chapter_numbers mc ON
    (mp.id IS NOT NULL AND mc.id = mp.manga_chapter_id) OR
    (mp.id IS NULL AND mmc.first_chapter_id = mc.id)
LEFT JOIN manga_bookmarks mb ON mb.manga_chapter_id = mc.id
WHERE
	m.deleted_at IS NULL AND
	mp.deleted_at IS NULL
ORDER BY mp.updated_at DESC";

			var res = await _sql.QueryAsync<DbManga, DbMangaProgress, DbMangaChapter, MangaStats>(QUERY, new { platformId });
			return res.Select(t => new MangaProgress(t.item1, t.item2, t.item3, t.item4)).ToArray();
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
				.ToArray();
		}

		public async Task<PaginatedResult<DbManga>> Search(MangaFilter filter)
		{
			const string QUERY = @"SELECT m.*
FROM manga m
WHERE {0}
ORDER BY m.title {1}
LIMIT :size OFFSET :offset;
SELECT COUNT(*) FROM manga m WHERE {0};";

			var parts = new List<string>();
			var pars = new DynamicParameters();
			pars.Add("offset", (filter.Page - 1) * filter.Size);
			pars.Add("size", filter.Size);

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

			var query = string.Format(QUERY, where, sort);
			using var con = _sql.CreateConnection();
			using var rdr = await con.QueryMultipleAsync(query, pars);

			var res = (await rdr.ReadAsync<DbManga>()).ToArray();
			var total = await rdr.ReadSingleAsync<int>();
			var pages = (long)Math.Ceiling((double)total / filter.Size);
			return new PaginatedResult<DbManga>(pages, total, res);
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
	}
}
