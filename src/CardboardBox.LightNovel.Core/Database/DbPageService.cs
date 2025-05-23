﻿using System.Linq.Expressions;

namespace CardboardBox.LightNovel.Core.Database;

using Anime.Database.Generation;

public interface IDbPageService : ILnOrmMap<Page>
{
	Task<PaginatedResult<Page>> Paginate(long seriesId, int page = 1, int size = 100);
	Task<Page?> LastPage(long seriesId);
	Task<Page[]> ImagePages();
	Task<Page[]> AnchorPages();
	Task<Page?> ByHashId(string hashId);
}

public class DbPageService : LnOrmMap<Page>, IDbPageService
{
	private string? _paginateQuery;

	public override string TableName => "ln_pages";

	public override Expression<Func<Page, long>> FkId => t => t.SeriesId;

	public DbPageService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public Task<PaginatedResult<Page>> Paginate(long seriesId, int page = 1, int size = 100)
	{
		_paginateQuery ??= _query.Pagniate<Page, long>(TableName, t => t.With(a => a.SeriesId), t => t.Ordinal, true);
		return Paginate(_paginateQuery, new { seriesId }, page, size);
	}

	public async Task<Page?> LastPage(long seriesId)
	{
		const string QUERY = @"SELECT *
FROM ln_pages 
WHERE 
	series_id = :seriesId AND 
	(
		next_url IS NULL OR 
		next_url = ''
	) 
ORDER BY ordinal DESC 
LIMIT 1";
		var res = await _sql.Fetch<Page?>(QUERY, new { seriesId });

		if (res != null) return res;

		const string BACKUP_QUERY = @"SELECT * 
FROM ln_pages 
WHERE series_id = :seriesId 
ORDER BY ordinal DESC 
LIMIT 1";
		return await _sql.Fetch<Page?>(BACKUP_QUERY, new { seriesId });
	}

	public Task<Page[]> ImagePages()
	{
		const string QUERY = "SELECT * FROM ln_pages WHERE content LIKE '%<img%';";
		return _sql.Get<Page>(QUERY);
    }

    public Task<Page[]> AnchorPages()
    {
        const string QUERY = "SELECT * FROM ln_pages WHERE content LIKE '%<a%';";
        return _sql.Get<Page>(QUERY);
    }

    public Task<Page?> ByHashId(string hashId)
    {
        const string QUERY = "SELECT * FROM ln_pages WHERE hash_id = :hashId";
        return _sql.Fetch<Page?>(QUERY, new { hashId });
    }
}
