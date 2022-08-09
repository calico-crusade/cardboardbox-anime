namespace CardboardBox.Anime.Database
{
	using Generation;

	public interface IListDbService
	{
		Task<long> Upsert(DbList list);
		Task<DbListExt[]> ByProfile(string id);
		Task<DbListExt[]> ByProfile(string id, long animeId);
		Task Update(DbList list);
		Task<DbList> Fetch(long id);
	}

	public class ListDbService : OrmMapExtended<DbList>, IListDbService
	{
		private string? _upsertQuery;

		public override string TableName => "lists";

		public ListDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<DbListExt[]> ByProfile(string id)
		{
			const string QUERY = @"SELECT 
	l.*,
	(
		SELECT
			COUNT(*)
		FROM list_map m 
		WHERE m.list_id = l.id
	) as count
FROM lists l 
JOIN profiles p ON p.id = l.profile_id 
WHERE
	p.platform_id = :id AND
	l.deleted_at IS NULL AND
	p.deleted_at IS NULL";

			return _sql.Get<DbListExt>(QUERY, new { id });
		}

		public Task<DbListExt[]> ByProfile(string id, long animeId)
		{
			const string QUERY = @"SELECT 
	l.*,
	(
		SELECT
			COUNT(*)
		FROM list_map m 
		WHERE m.list_id = l.id
	) as count
FROM lists l 
JOIN profiles p ON p.id = l.profile_id 
JOIN list_map lm ON lm.list_id = l.id
WHERE
	p.platform_id = :id AND
	lm.anime_id = :animeId AND
	l.deleted_at IS NULL AND
	p.deleted_at IS NULL AND
	lm.deleted_at IS NULL";

			return _sql.Get<DbListExt>(QUERY, new { id, animeId });
		}

		public Task<long> Upsert(DbList list)
		{
			_upsertQuery ??= _query.Upsert<DbList, long>(TableName,
				(v) => v.With(t => t.Title).With(t => t.ProfileId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertQuery, list);
		}
	}
}
