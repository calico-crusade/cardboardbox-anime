using Dapper;
using Microsoft.Extensions.Logging;

namespace CardboardBox.Anime.Database;

using Core;
using Core.Models;
using Generation;

public interface IMangaDbService
{
    #region For DbManga
    Task<DbManga[]> All();

    Task<long> Upsert(DbManga manga);

    Task<DbManga?> Get(string sourceId);

    Task<DbManga?> Get(long id);

    Task<DbManga?> GetByHashId(string hashId);

    Task<PaginatedResult<DbManga>> Paginate(int page = 1, int size = 100);

    Task<DbManga[]> FirstUpdated(int count);

    Task<DbManga[]> Random(int count);

	Task SetDisplayTitle(string id, string? title);

	Task SetOrdinalReset(string id, bool reset);
    #endregion

    #region For MangaWithChapters

    Task<MangaWithChapters?> GetManga(long id, string? platformId);

    Task<MangaWithChapters?> GetManga(string id, string? platformId);

    Task<MangaWithChapters?> Random(string? platformId);
    #endregion

    #region for DbMangaChapter
    Task<DbMangaChapter[]> AllChapters();

    Task<long> Upsert(DbMangaChapter chapter, bool updateComputed = true);

    Task<DbMangaChapter[]> Chapters(long mangaId, string language = "en");

    Task<DbMangaChapter?> GetChapter(long id);

    Task SetPages(long id, string[] pages);
    #endregion

    #region For DbMangaProgress

    Task<long> Upsert(DbMangaProgress progress);

    Task<DbMangaProgress?> GetProgress(string platformId, long mangaId);

    Task<DbMangaProgress?> GetProgress(string platformId, string mangaId);

    Task<DbMangaProgress[]> AllProgress();

    Task UpdateProgress(DbMangaProgress progress);

    Task<long> InsertProgress(DbMangaProgress progress);

    Task DeleteProgress(long profileId, long mangaId);
	#endregion

	#region For MangaProgress (technically Extended Manga information + Search)

	Task<MangaProgress?> GetMangaExtended(long id, string? platformId);

    Task<MangaProgress?> GetMangaExtended(string id, string? platformId);

    Task<PaginatedResult<MangaProgress>> Since(string? platform, DateTime since, int page, int size);

    Task<PaginatedResult<MangaProgress>> Search(MangaFilter filter, string? platformId, bool canRead);

    Task<Filter[]> Filters();
    #endregion

    #region For DbMangaBookmark
    Task<DbMangaBookmark[]> Bookmarks(long id, string? platformId);

    Task Bookmark(long id, long chapterId, int[] pages, string platformId);
    #endregion

    #region For DbMangaFavourites
    Task<bool?> Favourite(string platformId, long mangaId);

    Task<bool> IsFavourite(string? platformId, long mangaId);
    #endregion

	Task<GraphOut[]> Graphic(string? platformId, TouchedState state = TouchedState.Completed);

	Task UpdateComputed();

	Task UpdateChapterComputed();

	Task DeleteManga(long id);

	Task DeleteChapter(long id);

	Task FakeUpdate(long id);
}

public class MangaDbService : OrmMapExtended<DbManga>, IMangaDbService
{
	private const string TABLE_NAME_MANGA = "manga";
	private const string TABLE_NAME_MANGA_CHAPTER = "manga_chapter";
	private const string TABLE_NAME_MANGA_PROGRESS = "manga_progress";
	private const string TABLE_NAME_MANGA_BOOKMARKS = "manga_bookmarks";

	private static string? _getChapterQuery;
	private static string? _getMangaQuery;
	private static string? _getProgress;
	private static string? _insertProgress;
	private static string? _updateProgress;

	private static readonly List<string> _upsertChapters = new();
	private static readonly List<string> _upsertManga = new();
	private static readonly List<string> _upsertBookmark = new();
	private static readonly Random _rnd = new();
	private static readonly SemaphoreSlim _compute = new(1, 1);
	private static Task? _finishedTask = null;

	public override string TableName => TABLE_NAME_MANGA;

	private readonly IProfileDbService _prof;
	private readonly ILogger<MangaDbService> _logger;

	public MangaDbService(
		IDbQueryBuilderService query, 
		ISqlService sql, 
		IProfileDbService prof,
		ILogger<MangaDbService> logger) : base(query, sql) 
	{ 
		_prof = prof;
		_logger = logger;
	}

	public Task<DbMangaChapter[]> AllChapters()
	{
		return _sql.Get<DbMangaChapter>("SELECT * FROM " + TABLE_NAME_MANGA_CHAPTER);
	}

	public MangaSortField[] SortFields()
	{
		return new[]
		{
			new MangaSortField("Title", 0, "m.title"),
			new("Provider", 1, "m.provider"),
			new("Latest Chapter", 2, "p.latest_chapter"),
			new("Description", 3, "m.description"),
			new("Updated", 4, "m.updated_at"),
			new("Created", 5, "m.created_at")
		};
	}

	public async Task<long> Upsert(DbManga manga)
	{
		var id = await FakeUpsert(manga, TABLE_NAME_MANGA, _upsertManga,
			(v) => v.With(t => t.Provider).With(t => t.SourceId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt).With(t => t.Uploader).With(t => t.DisplayTitle).With(t => t.OrdinalVolumeReset));
        await UpdateComputed();
		return id;
    }

	public async Task<long> Upsert(DbMangaChapter chapter, bool updateComputed = true)
	{
		var id = await FakeUpsert(chapter, TABLE_NAME_MANGA_CHAPTER,
			_upsertChapters,
			(v) => v.With(t => t.MangaId).With(t => t.SourceId).With(t => t.Language),
			(v) => v.With(t => t.Id),
			v => v.With(t => t.Id).With(t => t.CreatedAt).With(t => t.Pages));
		if (updateComputed)
			await UpdateChapterComputed();
		return id;
    }

	public async Task<long> Upsert(DbMangaProgress progress)
	{
		_getProgress ??= _query.Select<DbMangaProgress>(TABLE_NAME_MANGA_PROGRESS, t => t.With(a => a.ProfileId).With(a => a.MangaId));
		_insertProgress ??= _query.InsertReturn<DbMangaProgress, long>(TABLE_NAME_MANGA_PROGRESS, t => t.Id, t => t.With(a => a.Id));
		_updateProgress ??= _query.Update<DbMangaProgress>(TABLE_NAME_MANGA_PROGRESS, t => t.With(a => a.Id).With(a => a.CreatedAt));

		var exists = await _sql.Fetch<DbMangaProgress>(_getProgress, new { progress.ProfileId, progress.MangaId });
		if (exists == null)
		{
			if (progress.MangaChapterId != null && progress.PageIndex != null)
				progress.Read = new[]
				{
					new DbMangaChapterProgress(progress.MangaChapterId.Value, progress.PageIndex.Value)
				};
			var id = await _sql.ExecuteScalar<long>(_insertProgress, progress);
			return id;
        }

		var pages = exists.Read;
		if (progress.MangaChapterId != null &&
			progress.PageIndex != null)
        {
			var cur = new DbMangaChapterProgress(progress.MangaChapterId.Value, progress.PageIndex.Value);
            var found = false;
            pages = exists.Read.Select(t =>
			{
				if (t.ChapterId != progress.MangaChapterId) return t;
				found = true;

				if (t.PageIndex > progress.PageIndex) return t;

				return cur;
			}).ToArray();

            if (!found)
                pages = pages.Append(cur).ToArray();
        }

		progress.Id = exists.Id;
		progress.Read = pages.OrderBy(t => t.ChapterId).ToArray();

		await _sql.Execute(_updateProgress, progress);
        return exists.Id;
	}

	public Task SetPages(long id, string[] pages)
	{
		const string QUERY = "UPDATE manga_chapter SET pages = :pages WHERE id = :id";
		return _sql.Execute(QUERY, new { id, pages });
    }

	public Task DeleteProgress(long profileId, long mangaId)
	{
		const string QUERY = @"UPDATE manga_progress 
SET 
	manga_chapter_id = NULL, 
	page_index = NULL 
WHERE 
	profile_id = :profileId AND 
	manga_id = :mangaId";
		return _sql.Execute(QUERY, new { profileId, mangaId });
    }

	public Task<DbMangaProgress[]> AllProgress()
	{
		return _sql.Get<DbMangaProgress>("SELECT * FROM manga_progress");
	}

	public Task UpdateProgress(DbMangaProgress progress)
	{
		_updateProgress ??= _query.Update<DbMangaProgress>(TABLE_NAME_MANGA_PROGRESS, t => t.With(a => a.Id).With(a => a.CreatedAt));
		return _sql.Execute(_updateProgress, progress);
    }

	public Task<long> InsertProgress(DbMangaProgress progress)
	{
        _insertProgress ??= _query.InsertReturn<DbMangaProgress, long>(TABLE_NAME_MANGA_PROGRESS, t => t.Id, t => t.With(a => a.Id));
		return _sql.ExecuteScalar<long>(_insertProgress, progress);
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
ORDER BY volume, ordinal";
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
		if (long.TryParse(mangaId, out var id))
            return GetProgress(platformId, id);

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

	public async Task DeleteManga(long id)
	{
		const string QUERY = "UPDATE manga SET deleted_at = NOW() WHERE id = :id";
		await _sql.Execute(QUERY, new { id });
		await UpdateComputed();
	}

	public async Task DeleteChapter(long id)
	{
        const string QUERY = "UPDATE manga_chapter SET deleted_at = NOW() WHERE id = :id";
        await _sql.Execute(QUERY, new { id });
		await UpdateChapterComputed();
	}

	public async Task<Filter[]> Filters()
	{
		const string QUERY = @"WITH allTags as (
    SELECT
        DISTINCT
        'tag' as key,
        unnest(tags) as value,
        nsfw as nsfw
    FROM manga
), sfwTags as (
    SELECT
        DISTINCT
        key,
        LOWER(value) as value
    FROM allTags
	WHERE nsfw = False
), nsfwTags as (
    SELECT
        DISTINCT
        'nsfw-tag' as key,
        LOWER(value) as value
    FROM allTags
    WHERE LOWER(value) NOT IN (
        SELECT
            LOWER(value)
        FROM sfwTags
    )
), attributes AS (
    SELECT
        DISTINCT
        lower((attr).name) as key,
        (attr).value as value
    FROM (
        SELECT
            DISTINCT
            unnest(attributes) as attr
        FROM manga
    ) z
    WHERE (attr).name NOT IN ('Author', 'Artist')
), sources AS (
    SELECT
        DISTINCT
        'source' as key,
        provider as value
    FROM manga
)
SELECT
    *
FROM (
    SELECT * FROM sfwTags
    UNION ALL
    SELECT * FROM nsfwTags
    UNION ALL
    SELECT * FROM attributes
    UNION ALL
    SELECT * FROM sources
) x
ORDER BY key, value";
		var filters = await _sql.Get<DbFilter>(QUERY);
		return filters
			.GroupBy(t => t.Key, t => t.Value)
			.Select(t => new Filter(t.Key, t.ToArray()))
			.Append(new Filter("sorts", SortFields().Select(t => t.Name).ToArray()))
			.ToArray();
	}

	public string RandomSuffix(int length = 10)
	{
		var chars = "abcdefghijklmnopqrstuvwxyz";
		return new string(Enumerable.Range(0, 10).Select(t => chars[_rnd.Next(chars.Length)]).ToArray());
	}

	public async Task<PaginatedResult<MangaProgress>> Search(MangaFilter filter, string? platformId, bool canRead)
	{
		const string QUERY = @"
BEGIN;
DROP TABLE IF EXISTS search_manga_{3};

CREATE TEMP TABLE search_manga_{3} ON COMMIT DROP AS
SELECT
    DISTINCT
    p.*
FROM get_manga(:platformId, :state) p
JOIN manga m ON m.id = p.manga_id
LEFT JOIN manga_attributes a ON a.id = m.id
WHERE ( {0} ) AND
m.deleted_at IS NULL;

SELECT
    DISTINCT
    m.*,
    '' as split,
    mp.*,
    '' as split,
    mc.*,
    '' as split,
    p.*
FROM manga m
JOIN search_manga_{3} p ON p.manga_id = m.id
JOIN manga_stats s ON s.manga_id = m.id
LEFT JOIN manga_attributes a ON a.id = m.id
LEFT JOIN manga_progress mp ON mp.manga_id = m.id AND mp.profile_id = p.profile_id
JOIN manga_chapter mc ON p.manga_chapter_id = mc.id
JOIN manga_chapter lc ON lc.id = s.last_chapter_id
WHERE
	m.deleted_at IS NULL
ORDER BY {2} {1}
LIMIT :size OFFSET :offset;

SELECT COUNT(*) FROM search_manga_{3};

DROP TABLE search_manga_{3};
COMMIT;";

        var sortField = SortFields().FirstOrDefault(t => t.Id == (filter.Sort ?? 0))?.SqlName ?? "m.title";

        var parts = new List<string>();
        var pars = new DynamicParameters();
        pars.Add("offset", (filter.Page - 1) * filter.Size);
        pars.Add("size", filter.Size);
        pars.Add("platformId", platformId);
        pars.Add("state", (int)filter.State);

        if (filter.Attributes != null && filter.Attributes.Length > 0)
        {
            for (var i = 0; i < filter.Attributes.Length; i++)
            {
                var attr = filter.Attributes[i];
                var name = $"attr{i}";
                var type = attr.Type switch
                {
                    AttributeType.ContentRating => "content rating",
                    AttributeType.Status => "status",
                    AttributeType.OriginalLanguage => "original language",
                    _ => null
                };

                if (type == null || attr.Values == null || attr.Values.Length == 0) continue;

                pars.Add(name + "val", attr.Values);
                pars.Add(name + "type", type);

                if (attr.Include)
                {
                    parts.Add($"(LOWER(a.name) = :{name}type AND a.value = ANY( :{name}val ))");
                    continue;
                }

                parts.Add($"(LOWER(a.name) = :{name}type AND NOT (a.value = ANY( :{name}val )))");
            }
        }

        if (!string.IsNullOrEmpty(filter.Search))
        {
            parts.Add("m.fts @@ phraseto_tsquery('english', :search)");
            pars.Add("search", filter.Search);
        }

        if (filter.Sources != null && filter.Sources.Length > 0)
        {
            parts.Add("m.provider = ANY( :source )");
            pars.Add("source", filter.Sources);
        }

        if (filter.Include != null && filter.Include.Length > 0)
        {
            parts.Add("(LOWER(m.tags::text)::text[]) @> :include");
            pars.Add("include", filter.Include);
        }

        if (filter.Exclude != null && filter.Exclude.Length > 0)
        {
            parts.Add("NOT ((LOWER(m.tags::text)::text[]) && :exclude )");
            pars.Add("exclude", filter.Exclude);
        }

        if (filter.Nsfw != NsfwCheck.DontCare)
        {
            parts.Add("m.nsfw = :nsfw");
            pars.Add("nsfw", filter.Nsfw == NsfwCheck.Nsfw);
        }

        parts.Add("m.deleted_at IS NULL");
        var where = string.Join(" AND ", parts);
        var sort = filter.Ascending ? "ASC" : "DESC";

		using var con = _sql.CreateConnection();

		var query = string.Format(QUERY, where, sort, sortField, RandomSuffix());
		using var multi = await con.QueryMultipleAsync(query, pars);

		var result = multi.Read<DbManga, DbMangaProgress, DbMangaChapter, MangaStats, MangaProgress>(
			func: (m, p, c, s) => new MangaProgress(m, p, c, s),
			splitOn: "split").ToArray();
		if (!canRead)
			result.Each(t => t.Chapter.Pages = Array.Empty<string>());

		var total = await multi.ReadSingleAsync<int>();
        var pages = (long)Math.Ceiling((double)total / filter.Size);
        return new PaginatedResult<MangaProgress>(pages, total, result);
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
		if (pages.Length == 0)
		{
			await _sql.Execute(DELETE_QUERY, new { id, chapterId, pages, platformId });
            return;
		}

		var pid = await _prof.Fetch(platformId);
		if (pid == null) return;

		await FakeUpsert(new DbMangaBookmark
		{
			ProfileId = pid.Id,
			MangaId = id,
			MangaChapterId = chapterId,
			Pages = pages
		}, TABLE_NAME_MANGA_BOOKMARKS, _upsertBookmark, 
			v => v.With(t => t.ProfileId).With(t => t.MangaId).With(t => t.MangaChapterId),
			v => v.With(t => t.Id),
			v => v.With(t => t.Id).With(t => t.CreatedAt));
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
		const string QUERY = "SELECT * FROM manga WHERE id = :id AND deleted_at IS NULL;" +
            "SELECT * FROM manga_chapter WHERE manga_id = :id AND deleted_at IS NULL ORDER BY volume, ordinal ASC, created_at ASC;";
		const string TARGETED_QUERY = @"SELECT DISTINCT mb.* 
FROM manga_bookmarks mb
JOIN profiles p ON p.id = mb.profile_id
JOIN manga_chapter mc ON mc.id = mb.manga_chapter_id
WHERE 
	p.platform_id = :platformId AND 
	mb.manga_id = :id AND
	mc.deleted_at IS NULL
ORDER BY mb.manga_chapter_id;

SELECT DISTINCT 1 
FROM manga_favourites mf 
JOIN profiles p ON p.id = mf.profile_id
JOIN manga m ON m.id = mf.manga_id
WHERE 
	p.platform_id = :platformId AND 
	mf.manga_id = :id AND
	m.deleted_at IS NULL";

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
		if (long.TryParse(id, out long mid))
			return await GetManga(mid, platformId);

		const string QUERY = "SELECT * FROM manga WHERE hash_id = :id AND deleted_at IS NULL;" +
			@"SELECT c.* FROM manga_chapter c
JOIN manga m ON m.id = c.manga_id
WHERE 
	m.hash_id = :id AND
	m.deleted_at IS NULL AND
	c.deleted_at IS NULL
ORDER BY c.volume, c.ordinal ASC, c.created_at ASC;";
		const string TARGETED_QUERY = @"SELECT DISTINCT mb.* 
FROM manga_bookmarks mb
JOIN manga m ON m.id = mb.manga_id
JOIN profiles p ON p.id = mb.profile_id
JOIN manga_chapter mc ON mc.id = mb.manga_chapter_id
WHERE 
	p.platform_id = :platformId AND 
	m.hash_id = :id AND
    m.deleted_at IS NULL AND
    mc.deleted_at IS NULL
ORDER BY mb.manga_chapter_id;

SELECT 1 
FROM manga_favourites mf 
JOIN manga m ON m.id = mf.manga_id
JOIN profiles p ON p.id = mf.profile_id
WHERE 
	p.platform_id = :platformId AND 
	m.hash_id = :id AND
	m.deleted_at IS NULL";

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
		const string QUERY = @"SELECT *
FROM manga
WHERE provider NOT IN ('mangadex', 'mangakakalot-com')
ORDER BY updated_at ASC
LIMIT :count";
		return _sql.Get<DbManga>(QUERY, new { count }); 
	}

	public Task FakeUpdate(long id)
	{
		const string QUERY = "UPDATE manga SET updated_at = CURRENT_TIMESTAMP WHERE id = :id";
		return _sql.Execute(QUERY, new { id });
    }

	//
	public async Task<MangaWithChapters?> Random(string? platformId)
	{
		const string RANDOM_QUERY = "SELECT * FROM manga ORDER BY random() LIMIT 1;";
		const string QUERY = "SELECT * FROM manga_chapter WHERE manga_id = :id ORDER BY volume, ordinal;";
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

	public Task<DbManga[]> Random(int count)
	{
		return _sql.Get<DbManga>("SELECT * FROM manga ORDER BY random() LIMIT :count", new { count });
	}

	public async Task<PaginatedResult<MangaProgress>> Since(string? platformId, DateTime since, int page, int size)
	{
		const string QUERY = @"
BEGIN;
CREATE TEMP TABLE touched_manga ON COMMIT DROP AS
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

DROP TABLE touched_manga;
COMMIT;";

		var state = string.IsNullOrEmpty(platformId) ? TouchedState.All : TouchedState.InProgress;
		var offset = (page - 1) * size;
		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { platformId, state = (int)state, offset, size, since });

		var results = rdr.Read<DbManga, DbMangaProgress, DbMangaChapter, MangaStats, MangaProgress>((m, p, c, s) => new MangaProgress(m, p, c, s), splitOn: "split");
		var total = await rdr.ReadSingleAsync<int>();
		var pages = (long)Math.Ceiling((double)total / size);
		return new PaginatedResult<MangaProgress>(pages, total, results.ToArray());
	}

	public Task<MangaProgress?> GetMangaExtended(long id, string? platformId)
	{
		return GetMangaExtended(null, id, platformId);
	}

	public Task<MangaProgress?> GetMangaExtended(string id, string? platformId)
	{
		return GetMangaExtended(id, null, platformId);
	}

	public async Task<MangaProgress?> GetMangaExtended(string? hashId, long? id, string? platformId)
	{
		const string QUERY = @"SELECT
    m.*,
    '' as split,
    mp.*,
    '' as split,
    mc.*,
    '' as split,
    t.*
FROM get_manga_filtered( :platformId , 99, ARRAY(
	SELECT
	    id
    FROM manga
    WHERE
        (hash_id = :hashId OR id = :id) AND
	    deleted_at IS NULL
)) t
JOIN manga m ON m.id = t.manga_id
JOIN manga_chapter mc ON mc.id = t.manga_chapter_id
LEFT JOIN manga_progress mp ON mp.id = t.progress_id";

		using var con = _sql.CreateConnection();
		var records = await con.QueryAsync<DbManga, DbMangaProgress, DbMangaChapter, MangaStats, MangaProgress>(
			QUERY, (m, p, c, s) => new MangaProgress(m, p, c, s), 
			param: new { hashId, id, platformId }, splitOn: "split");
		return records.FirstOrDefault();
	}

	public Task<GraphOut[]> Graphic(string? platformId, TouchedState state = TouchedState.Completed)
	{
		const string QUERY = @"
BEGIN;
CREATE TEMP TABLE touched_manga ON COMMIT DROP AS
SELECT DISTINCT manga_id
FROM get_manga(:platformId, :state);

SELECT
    'tag' as type,
    x.tag as key,
    COUNT(*) as count
FROM (
    SELECT unnest(m.tags) as tag
    FROM touched_manga t
    JOIN manga m ON m.id = t.manga_id
) x
JOIN (
    SELECT DISTINCT unnest(tags) as tag
    FROM manga
    WHERE nsfw = false
) n ON n.tag = x.tag
GROUP BY x.tag
ORDER BY COUNT(*) DESC;
COMMIT;";
		return _sql.Get<GraphOut>(QUERY, new { platformId, state });
	}

	public async Task UpdateComputed()
	{
		if (_compute.CurrentCount < 1 && _finishedTask is not null)
		{
			_logger.LogInformation("Doing breakout stuff");
			await _finishedTask;
			return;
		}

		await _compute.WaitAsync();
		var tsc = new TaskCompletionSource();
		_finishedTask = tsc.Task;
		try
		{
			await _sql.Execute("CALL update_computed()");
			_logger.LogInformation("Updated computed fields for manga");
		}
		catch(Exception ex)
		{
			_logger.LogError(ex, "Error updating computed fields");
		}
		finally
		{
			tsc.SetResult();
			_finishedTask = null;
			_compute.Release();
		}
	}

	public Task UpdateChapterComputed()
	{
		return _sql.Execute("CALL update_chapter_computed()");
	}

	public Task SetDisplayTitle(string id, string? title)
	{
		const string QUERY = @"UPDATE manga SET display_title = :title WHERE id = :id OR hash_id = :hashId";

		long? mid = null;
		string? hashId = id;
		if (long.TryParse(id, out var m))
		{
            hashId = null;
			mid = m;
		}

		return _sql.Execute(QUERY, new { id = mid, hashId, title });
	}

	public Task SetOrdinalReset(string id, bool reset)
    {
        const string QUERY = @"UPDATE manga SET ordinal_volume_reset = :reset WHERE id = :id OR hash_id = :hashId";

        long? mid = null;
        string? hashId = id;
        if (long.TryParse(id, out var m))
        {
            hashId = null;
            mid = m;
        }

        return _sql.Execute(QUERY, new { id = mid, hashId, reset });
    }
}

public record class MangaSortField(string Name, int Id, string SqlName);

public class GraphOut
{
	public string Type { get; set; } = string.Empty;
	public string Key { get; set; } = string.Empty;
	public int Count { get; set; }
}
