using Dapper;

namespace CardboardBox.Anime.Database;

using Generation;

public interface IListDbService
{
	Task<long> Upsert(DbList list);

	Task<DbListExt[]> ByProfile(string platformId);
}

public class ListDbService : OrmMapExtended<DbList>, IListDbService
{
	private string? _upsertQuery;

	public override string TableName => "lists";

	public ListDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public Task<long> Upsert(DbList list)
	{
		_upsertQuery ??= _query.Upsert<DbList, long>(TableName,
			(v) => v.With(t => t.Title).With(t => t.ProfileId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt),
			(v) => v.Id);

		return _sql.ExecuteScalar<long>(_upsertQuery, list);
	}

	public async Task<DbListExt[]> ByProfile(string platformId)
	{
		const string QUERY = @"SELECT
    l.*,
    (
        SELECT
            COUNT(*)
        FROM list_items
        WHERE list_id = l.id
    )
FROM lists l
JOIN profiles p on l.profile_id = p.id
WHERE
    p.platform_id = :platformId
ORDER BY l.id;

SELECT
    li.list_id,
    li.type,
    COUNT(*) as count
FROM list_items li
JOIN lists l ON li.list_id = l.id
JOIN profiles p ON l.profile_id = p.id
WHERE
    p.platform_id = :platformId
GROUP BY li.list_id, li.type
ORDER BY li.list_id;";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { platformId });

		var results = (await rdr.ReadAsync<DbListExt>()).ToArray();
		var counts = (await rdr.ReadAsync<DbListCount>()).ToArray();

		for(int r = 0, c = 0; r < results.Length && c < counts.Length;)
		{
			var result = results[r];
			var count = counts[c];
			if (result.Id == count.ListId)
			{
				result.Counts.Add(count);
				c++;
				continue;
			}

			r++;
		}

		return results;
	}
}
