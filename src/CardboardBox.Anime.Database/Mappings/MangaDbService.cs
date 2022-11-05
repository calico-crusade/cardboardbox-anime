using Dapper;

namespace CardboardBox.Anime.Database
{
	using CardboardBox.Database;
	using Core;
	using Core.Models;
	using Generation;

	public interface IMangaDbService
	{
		Task<long> Upsert(DbManga manga);

		Task<DbManga?> Get(string url);
	}

	public class MangaDbService : OrmMapExtended<DbManga>, IMangaDbService
	{
		private string? _upsertQuery;

		public override string TableName => "manga";

		public MangaDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<long> Upsert(DbManga manga)
		{
			_upsertQuery ??= _query.Upsert<DbManga, long>(TableName,
				(v) => v.With(t => t.Provider).With(t => t.SourceId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertQuery, manga);
		}

		public Task<DbManga?> Get(string url)
		{
			const string QUERY = @"SELECT * FROM manga WHERE LOWER(url) = LOWER(:url) AND deleted_at IS NULL";
			return _sql.Fetch<DbManga?>(QUERY, new { url });
		}
	}
}
