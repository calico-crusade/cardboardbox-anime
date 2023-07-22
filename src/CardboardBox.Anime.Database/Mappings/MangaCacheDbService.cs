namespace CardboardBox.Anime.Database;

using CardboardBox.Database;
using Generation;

public interface IMangaCacheDbService
{
	Task<DbManga[]> All();

	Task<DbMangaChapter[]> AllChapters();

	Task<long> Upsert(DbManga manga);

	Task<long> Upsert(DbMangaChapter chapter);

	Task<MangaCache[]> DetermineExisting(string[] chapterIds);

	Task<DbManga[]> ByIds(string[] mangaIds);

	Task MergeUpdates();

	Task<DbManga[]> BadCoverArt();
}

public class MangaCacheDbService : OrmMapExtended<DbManga>, IMangaCacheDbService
{
	public const string TABLE_NAME_MANGA = "manga_cache";
	public const string TABLE_NAME_MANGA_CHAPTER = "manga_chapter_cache";

	public override string TableName => "manga_cache";

	private List<string> _upsertChapters = new();
	private List<string> _upsertManga = new();

	public MangaCacheDbService(
		IDbQueryBuilderService query,
		ISqlService sql) : base(query, sql) { }

	public Task<DbMangaChapter[]> AllChapters()
	{
		const string QUERY = "SELECT * FROM " + TABLE_NAME_MANGA_CHAPTER;
		return _sql.Get<DbMangaChapter>(QUERY);
	}

	public Task<long> Upsert(DbManga manga)
	{
		return FakeUpsert(manga, TABLE_NAME_MANGA, _upsertManga,
			(v) => v.With(t => t.Provider).With(t => t.SourceId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt));
	}

	public Task<long> Upsert(DbMangaChapter chapter)
	{
		return FakeUpsert(chapter, TABLE_NAME_MANGA_CHAPTER,
			_upsertChapters,
			(v) => v.With(t => t.MangaId).With(t => t.SourceId).With(t => t.Language),
			(v) => v.With(t => t.Id),
			v => v.With(t => t.Id).With(t => t.CreatedAt));
	}

	public Task<DbManga[]> ByIds(string[] mangaIds)
	{
		const string QUERY = @"SELECT
	DISTINCT
	*
FROM manga_cache
WHERE source_id = ANY(:mangaIds)";
		return _sql.Get<DbManga>(QUERY, new { mangaIds });
	}

	public async Task<MangaCache[]> DetermineExisting(string[] chapterIds)
	{
		const string QUERY = @"SELECT
	DISTINCT
    m.*,
    '' as split,
    mc.*,
    '' as split,
    om.*,
    '' as split,
    oc.*
FROM manga_cache m
JOIN manga_chapter_cache mc on m.id = mc.manga_id
LEFT JOIN manga om ON om.source_id = m.source_id AND om.provider = m.provider
LEFT JOIN manga_chapter oc on oc.source_id = mc.source_id AND oc.manga_id = om.id
WHERE
    mc.source_id = ANY(:chapterIds)";

		var records = await _sql.QueryAsync<DbManga, DbMangaChapter, DbManga, DbMangaChapter>(QUERY, new { chapterIds });

		return records.Select(t =>
		{
			var manga = t.item1;
			var chapter = t.item2;
			var cbamanga = t.item3.Title == null ? null : t.item3;
			var chaChapter = t.item4.Title == null ? null : t.item4;
			return new MangaCache(manga, chapter, cbamanga, chaChapter);
		}).ToArray();
	}

	public Task MergeUpdates()
	{
		const string QUERY = @"INSERT INTO manga_chapter
(
    manga_id,
    title,
    url,
    source_id,
    ordinal,
    volume,
    language,
    pages,
    external_url,
    created_at,
    updated_at
)
SELECT
    om.id as manga_id,
    mcc.title,
    mcc.url,
    mcc.source_id,
    mcc.ordinal,
    mcc.volume,
    mcc.language,
    mcc.pages,
    mcc.external_url,
    CURRENT_TIMESTAMP as created_at,
    CURRENT_TIMESTAMP as updated_at
FROM manga_chapter_cache mcc
JOIN manga_cache mc ON mc.id = mcc.manga_id
JOIN manga om ON om.source_id = mc.source_id AND om.provider = mc.provider
LEFT JOIN manga_chapter omc ON omc.source_id = mcc.source_id AND om.id = omc.manga_id
WHERE
    omc.id IS NULL;
CALL update_computed();";
		return _sql.Execute(QUERY);
	}

	public Task<DbManga[]> BadCoverArt()
	{
		const string QUERY = "SELECT * FROM manga_cache WHERE cover LIKE '%/';";
		return _sql.Get<DbManga>(QUERY);
	}
}

public record class MangaCache(DbManga manga, DbMangaChapter chapter, DbManga? cbaManga, DbMangaChapter? DbMangaChapter);