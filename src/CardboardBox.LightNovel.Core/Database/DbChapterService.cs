namespace CardboardBox.LightNovel.Core.Database
{
	using Anime.Database.Generation;

	public interface IDbChapterService : ILnOrmMap<Chapter>
	{
		Task<Chapter[]> ByBook(long bookId);
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
	}
}
