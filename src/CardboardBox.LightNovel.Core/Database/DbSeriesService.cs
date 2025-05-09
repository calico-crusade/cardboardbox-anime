﻿namespace CardboardBox.LightNovel.Core.Database;

using Anime.Database.Generation;

public interface IDbSeriesService
{
	Task<PaginatedResult<Series>> Paginate(int page = 1, int size = 100);
	Task<PartialScaffold?> PartialScaffold(long seriesId);
	Task<FullScaffold?> Scaffold(long seriesId);
	Task<Series?> FromUrl(string url);
	Task Delete(long seriesId);
	Task<long> Upsert(Series series);
	Task<Series> Fetch(long id);
	Task<long> Insert(Series obj);
	Task Update(Series obj);
	Task<Series[]> All();
}

public class DbSeriesService : OrmMapExtended<Series>, IDbSeriesService
{
	private string? _paginateQuery;
	private string? _fromUrlQuery;
	private string? _upsertQuery;

	public override string TableName => "ln_series";

	public DbSeriesService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public override Task<PaginatedResult<Series>> Paginate(int page = 1, int size = 100)
	{
		_paginateQuery ??= _query.Pagniate<Series, string>(TableName, c => { }, t => t.Title);
		return Paginate(_paginateQuery, null, page, size);
	}

	public async Task<PartialScaffold?> PartialScaffold(long seriesId)
	{
		const string QUERY = @"SELECT * FROM ln_series WHERE id = :seriesId;

SELECT * FROM ln_books WHERE series_id = :seriesId ORDER BY ordinal;

SELECT c.* FROM ln_chapters c
JOIN ln_books b ON b.id = c.book_id
WHERE b.series_id = :seriesId
ORDER BY b.ordinal, c.ordinal;";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { seriesId });

		var series = await rdr.ReadFirstOrDefaultAsync<Series>();
		if (series == null) return null;

		var books = (await rdr.ReadAsync<Book>()).ToArray();
		var chapters = (await rdr.ReadAsync<Chapter>()).ToGDictionary(t => t.BookId, t => t.Ordinal);

		return new PartialScaffold
		{
			Series = series,
			Books = books.Select(t =>
			{
				var chaps = chapters.ContainsKey(t.Id) ? chapters[t.Id] : Array.Empty<Chapter>();
				return new PartialBookScaffold
				{
					Book = t,
					Chapters = chaps
				};
			}).ToArray()
		};
	}

	public async Task<FullScaffold?> Scaffold(long seriesId)
	{
		const string QUERY = @"
SELECT * FROM ln_series WHERE id = :seriesId;

SELECT 
	* 
FROM ln_books 
WHERE series_id = :seriesId
ORDER BY ordinal;

SELECT 
	c.* 
FROM ln_chapters c 
JOIN ln_books b ON b.id = c.book_id 
WHERE b.series_id = :seriesId
ORDER BY b.ordinal, c.ordinal;";

		const string QUERY2 = @"
SELECT 
	p.*,
	'' as split,
	cp.*
FROM ln_pages p
JOIN ln_chapter_pages cp ON cp.page_id = p.id
JOIN ln_chapters c ON c.id = cp.chapter_id
JOIN ln_books b ON b.id = c.book_id
WHERE b.series_id = :seriesId
ORDER BY b.ordinal, c.ordinal, cp.ordinal";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { seriesId });

		var series = await rdr.ReadFirstOrDefaultAsync<Series>();
		if (series == null) return null;

		var books = (await rdr.ReadAsync<Book>()).OrderBy(t => t.Ordinal).ToArray();
		var chapters = (await rdr.ReadAsync<Chapter>()).ToGDictionary(t => t.BookId, t => t.Ordinal);

		var map = (await con.QueryAsync<Page, ChapterPage, PageScaffold>(QUERY2, (p, c) => new PageScaffold
		{
			Page = p,
			Map = c
		}, new { seriesId }, splitOn: "split")).ToGDictionary(t => t.Map.ChapterId, t => t.Map.Ordinal);

		return new FullScaffold
		{
			Series = series,
			Books = books.Select(t => new BookScaffold
			{
				Book = t,
				Chapters = !chapters.ContainsKey(t.Id) ? Array.Empty<ChapterScaffold>() :
					chapters[t.Id].Select(a => new ChapterScaffold
					{
						Chapter = a,
						Pages = !map.ContainsKey(a.Id) ? Array.Empty<PageScaffold>() : map[a.Id]
					}).ToArray()
			}).ToArray()
		};
	}

	public Task<Series?> FromUrl(string url)
	{
		_fromUrlQuery ??= _query.Select<Series>(TableName, t => t.With(a => a.Url));
		return _sql.Fetch<Series?>(_fromUrlQuery, new { url });
	}

	public Task Delete(long seriesId)
	{
		const string QUERY = @"
DELETE FROM ln_chapter_pages WHERE chapter_id IN (
    SELECT c.id FROM ln_chapters c
    JOIN ln_books b ON c.book_id = b.id
    WHERE b.series_id = :seriesId
);

DELETE FROM ln_chapters WHERE book_id IN (
    SELECT id FROM ln_books WHERE series_id = :seriesId
);

DELETE FROM ln_books WHERE series_id = :seriesId;
DELETE FROM ln_pages WHERE series_id = :seriesId;
DELETE FROM ln_series WHERE id = :seriesId;";

		return _sql.Execute(QUERY, new { seriesId });
	}

	public Task<long> Upsert(Series item)
	{
		_upsertQuery ??= _query.Upsert<Series, long>(TableName,
			(v) => v.With(t => t.HashId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt),
			(v) => v.Id);

		return _sql.ExecuteScalar<long>(_upsertQuery, item);
	}
}
