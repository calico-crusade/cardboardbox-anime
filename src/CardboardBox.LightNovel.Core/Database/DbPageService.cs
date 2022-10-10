namespace CardboardBox.LightNovel.Core.Database
{
	using Anime.Database.Generation;

	public interface IDbPageService : ILnOrmMap<Page>
	{
		Task<PaginatedResult<Page>> Paginate(long seriesId, int page = 1, int size = 100);
		Task<Page?> LastPage(long seriesId);
	}

	public class DbPageService : LnOrmMap<Page>, IDbPageService
	{
		private string? _paginateQuery;

		public override string TableName => "ln_pages";

		public DbPageService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<PaginatedResult<Page>> Paginate(long seriesId, int page = 1, int size = 100)
		{
			_paginateQuery ??= _query.Pagniate<Page, long>(TableName, t => t.With(a => a.SeriesId), t => t.Ordinal, true);
			return Paginate(_paginateQuery, new { seriesId }, page, size);
		}

		public Task<Page?> LastPage(long seriesId)
		{
			const string QUERY = @"SELECT * FROM ln_pages WHERE series_id = :seriesId AND (next_url IS NULL OR next_url = '')";
			return _sql.Fetch<Page?>(QUERY, new { seriesId });
		}
	}
}
