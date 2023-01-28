namespace CardboardBox.Anime.Database;

using Generation;

public interface IListMapDbService
{
	Task<long> Upsert(DbListMap listMap);
	Task<DbListMapItem[]> Get(string id);
	Task<bool> Toggle(long animeId, long listId);
}

public class ListMapDbService : OrmMapExtended<DbListMap>, IListMapDbService
{
	private string? _upsertQuery;
	private string? _selectQuery;

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

	public async Task<bool> Toggle(long animeId, long listId)
	{
		_selectQuery ??= _query.Select<DbListMap>(TableName, t => t.With(a => a.AnimeId).With(a => a.ListId));
		var target = await _sql.Fetch<DbListMap>(_selectQuery, new { animeId, listId });

		if (target == null || target.DeletedAt != null)
		{
			await Upsert(new DbListMap
			{
				AnimeId = animeId,
				ListId = listId,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now
			});
			return true;
		}

		target.DeletedAt = DateTime.Now;
		await Upsert(target);
		return false;
	}

	public async Task<DbListMapItem[]> Get(string id)
	{
		const string QUERY = @"SELECT
    lm.list_id,
    lm.anime_id
FROM lists l
JOIN list_map lm on l.id = lm.list_id
JOIN anime a on lm.anime_id = a.id
JOIN profiles p on l.profile_id = p.id
WHERE
    l.deleted_at IS NULL AND
    lm.deleted_at IS NULL AND
    a.deleted_at IS NULL AND
    p.deleted_at IS NULL AND
    p.platform_id = :id
ORDER BY lm.list_id";
		var maps = await _sql.Get<DbListMapStripped>(QUERY, new { id });
		return maps
			.GroupBy(t => t.ListId)
			.Select(t => new DbListMapItem
			{
				ListId = t.Key,
				AnimeIds = t.Select(a => a.AnimeId).ToArray()
			})
			.ToArray();
	}
}
