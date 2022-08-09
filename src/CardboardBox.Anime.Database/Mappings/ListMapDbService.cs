namespace CardboardBox.Anime.Database
{
	using Generation;

	public interface IListMapDbService
	{
		Task<long> Upsert(DbListMap listMap);
	}

	public class ListMapDbService : OrmMapExtended<DbListMap>, IListMapDbService
	{
		private string? _upsertQuery;

		public override string TableName => "list_map";

		public ListMapDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<long> Upsert(DbListMap listMap)
		{
			_upsertQuery ??= _query.Upsert<DbListMap, long>(TableName,
				(v) => v.With(t => t.ListId).With(t => t.AnimeId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertQuery, listMap);
		}
	}
}
