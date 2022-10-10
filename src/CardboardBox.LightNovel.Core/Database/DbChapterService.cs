namespace CardboardBox.LightNovel.Core.Database
{
	using Anime.Database.Generation;

	public interface IDbChapterService : ILnOrmMap<Chapter>
	{
		Task<Chapter[]> ByBook(long bookId);
		Task<(Book book, Chapter chap, ChapterPage page)> LastChapter(long seriesId);
	}

	public class DbChapterService : LnOrmMap<Chapter>, IDbChapterService
	{
		private string? _byBookQuery;

		public override string TableName => "ln_chapters";

		public DbChapterService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<Chapter[]> ByBook(long bookId)
		{
			_byBookQuery ??= _query.Select<Chapter>(TableName, (v) => v.With(t => t.BookId));
			return _sql.Get<Chapter>(_byBookQuery, new { bookId });
		}

		public async Task<(Book book, Chapter chap, ChapterPage page)> LastChapter(long seriesId)
		{
			const string QUERY = @"SELECT
	lb.*,
	'' as split,
    c.*,
    '' as split,
    lcp.*
FROM ln_chapters c
JOIN ln_books lb on c.book_id = lb.id
JOIN ln_chapter_pages lcp on c.id = lcp.chapter_id
JOIN ln_pages lp on lcp.page_id = lp.id
WHERE
    lb.series_id = :seriesId
ORDER BY lb.ordinal DESC, c.ordinal DESC, lcp.ordinal DESC
LIMIT 1";
			return (await _sql.QueryAsync<Book, Chapter, ChapterPage>(QUERY, new { seriesId })).FirstOrDefault();
		}
	}
}
