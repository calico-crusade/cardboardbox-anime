using System.Linq.Expressions;

namespace CardboardBox.LightNovel.Core.Database;

using Anime.Database.Generation;

public interface IDbBookService : ILnOrmMap<Book>
{
	Task<Book[]> BySeries(long seriesId);
	Task<FullBookScaffold?> Scaffold(long bookId);
}

public class DbBookService : LnOrmMap<Book>, IDbBookService
{
	private string? _bySeriesQuery;

	public override string TableName => "ln_books";

	public override Expression<Func<Book, long>> FkId => t => t.SeriesId;

	public DbBookService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public Task<Book[]> BySeries(long seriesId)
	{
		_bySeriesQuery ??= _query.Select<Book>(TableName, (v) => v.With(t => t.SeriesId));
		return _sql.Get<Book>(_bySeriesQuery, new { seriesId });
	}

	public async Task<FullBookScaffold?> Scaffold(long bookId)
	{
		const string QUERY = @"
SELECT
	DISTINCT 
	s.*
FROM ln_series s
JOIN ln_books b ON b.series_id = s.id
WHERE
	b.id = :bookId;

SELECT 
	* 
FROM ln_books 
WHERE id = :bookId
ORDER BY ordinal;

SELECT 
	c.* 
FROM ln_chapters c
WHERE c.book_id = :bookId
ORDER BY c.ordinal;";

		const string QUERY2 = @"
SELECT 
	p.*,
	'' as split,
	cp.*
FROM ln_pages p
JOIN ln_chapter_pages cp ON cp.page_id = p.id
JOIN ln_chapters c ON c.id = cp.chapter_id
WHERE c.book_id = :bookId
ORDER BY cp.ordinal";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { bookId });

		var series = await rdr.ReadFirstOrDefaultAsync<Series>();
		var book = await rdr.ReadFirstOrDefaultAsync<Book>();
		if (series == null || book == null) return null;

		var chapters = (await rdr.ReadAsync<Chapter>()).OrderBy(t => t.Ordinal).ToArray();

		var map = (await con.QueryAsync<Page, ChapterPage, PageScaffold>(QUERY2, (p, c) => new PageScaffold
		{
			Page = p,
			Map = c
		}, new { bookId }, splitOn: "split")).ToGDictionary(t => t.Map.ChapterId, t => t.Map.Ordinal);

		return new FullBookScaffold
		{
			Series = series,
			Book = book,
			Chapters = chapters.Select(a => new ChapterScaffold
			{
				Chapter = a,
				Pages = !map.ContainsKey(a.Id) ? Array.Empty<PageScaffold>() : map[a.Id]
			}).ToArray()
		};
	}
}
