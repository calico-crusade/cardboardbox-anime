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

		public async Task<MangaProgress[]> InProgress(string platformId)
		{
			const string QUERY = @"WITH chapter_numbers AS (
    SELECT
        *,
        row_number() over (
            PARTITION BY manga_id
            ORDER BY ordinal ASC
        ) as row_num
    FROM manga_chapter c
), max_chapter_numbers AS (
    SELECT
        c.manga_id,
        MAX(c.row_num) as max
    FROM chapter_numbers c
    GROUP BY c.manga_id
) SELECT
	m.*,
	'' as split,
	mp.*,
	'' as split,
	mc.*,
	'' as split,
	mmc.max as max_chapter_num,
    mc.row_num as chapter_num,
    array_length(mc.pages, 1) as page_count,
    round(mc.row_num / CAST(mmc.max as decimal) * 100, 2) as chapter_progress,
    round(mp.page_index / CAST(array_length(mc.pages, 1) as decimal), 2) * 100 as page_progress
FROM manga m
JOIN manga_progress mp ON mp.manga_id = m.id
JOIN chapter_numbers mc ON mc.id = mp.manga_chapter_id
JOIN max_chapter_numbers mmc ON mmc.manga_id = mc.manga_id
JOIN profiles p ON p.id = mp.profile_id
WHERE
	p.platform_id = :platformId AND
	m.deleted_at IS NULL AND
	mp.deleted_at IS NULL AND
	p.deleted_at IS NULL
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
	}
}
