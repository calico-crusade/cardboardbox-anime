namespace CardboardBox.LightNovel.Core.Database
{
	using Anime.Database.Generation;

	public interface IDbChapterPageService
	{
		Task<long> Upsert(ChapterPage page);

		Task<ChapterPage> Fetch(long id);

		Task<long> Insert(ChapterPage obj);

		Task Update(ChapterPage obj);

		Task<(Page page, ChapterPage map)[]> Chapter(long chapterId);
	}

	public class DbChapterPageService : OrmMapExtended<ChapterPage>, IDbChapterPageService
	{
		private string? _upsertQuery;

		public override string TableName => "ln_chapter_pages";

		public DbChapterPageService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<long> Upsert(ChapterPage item)
		{
			_upsertQuery ??= _query.Upsert<ChapterPage, long>(TableName,
				(v) => v.With(t => t.ChapterId).With(t => t.PageId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertQuery, item);
		}

		public async Task<(Page page, ChapterPage map)[]> Chapter(long chapterId)
		{
			const string QUERY = @"
SELECT 
	*,
	'' as split,
	cp.*
FROM ln_pages p
JOIN ln_chapter_pages cp ON cp.page_id = p.id
WHERE
	cp.chapter_id = :chapterId
ORDER BY cp.ordinal ASC";

			using var con = _sql.CreateConnection();
			return (await con.QueryAsync<Page, ChapterPage, (Page page, ChapterPage chapter)>(QUERY, 
				(p, c) => (p, c),
				new { chapterId })).ToArray();
		}
	}
}
