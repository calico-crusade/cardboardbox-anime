namespace CardboardBox.LightNovel.Core.Database
{
	using Anime.Database.Generation;

	public interface IDbBookService : ILnOrmMap<Book>
	{
		Task<Book[]> BySeries(long seriesId);
	}

	public class DbBookService : LnOrmMap<Book>, IDbBookService
	{
		private string? _bySeriesQuery;

		public override string TableName => "ln_books";

		public DbBookService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<Book[]> BySeries(long seriesId)
		{
			_bySeriesQuery ??= _query.Select<Book>(TableName, (v) => v.With(t => t.SeriesId));
			return _sql.Get<Book>(_bySeriesQuery, new { seriesId });
		}
	}
}
