namespace CardboardBox.Anime.Database
{
	using CardboardBox.Database;
	using Generation;

	public interface IMangaCacheDbService
	{
		Task<long> Upsert(DbManga manga);

		Task<long> Upsert(DbMangaChapter chapter);

		Task<MangaCache[]> DetermineExisting(string[] chapterIds);
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
	}

	public record class MangaCache(DbManga manga, DbMangaChapter chapter, DbManga? cbaManga, DbMangaChapter? DbMangaChapter);
}