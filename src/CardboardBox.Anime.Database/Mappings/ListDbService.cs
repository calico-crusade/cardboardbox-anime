using Dapper;

namespace CardboardBox.Anime.Database;

using Generation;

public interface IListDbService
{
	Task<long> Upsert(DbList list);
	Task<DbListExt[]> ByProfile(string id);
	Task<DbListExt[]> ByProfile(string id, long animeId);
	Task Update(DbList list);
	Task<DbList> Fetch(long id);
	Task<DbListExt> Get(string? id, long listId);
	Task<(CompPublicList[] results, long count)> PublicLists(long page = 1, long size = 100);
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
		WHERE 
			m.list_id = l.id AND 
			m.deleted_at IS NULL
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
		WHERE 
			m.list_id = l.id AND
			m.deleted_at IS NULL
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

	public Task<DbListExt> Get(string? id, long listId)
	{
		const string QUERY = @"
SELECT
	l.*,
	(
		SELECT
			COUNT(*)
		FROM list_map m 
		WHERE 
			m.list_id = l.id AND 
			m.deleted_at IS NULL
	) as count
FROM lists l
JOIN profiles p ON p.id = l.profile_id
WHERE
	l.deleted_at IS NULL AND
	p.deleted_at IS NULL AND
	l.id = :listId AND 
	(
		p.platform_id = :id OR
		l.is_public = true
	)";
		return _sql.Fetch<DbListExt>(QUERY, new { id, listId });
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

	public async Task<(CompPublicList[] results, long count)> PublicLists(long page = 1, long size = 100)
	{
		var offset = (page - 1) * size;
		var query = $@"SELECT
	DISTINCT
    l.id as list_id,
    l.title as list_title,
    l.description as list_description,
    (
        SELECT MAX(lm.created_at) FROM list_map lm WHERE lm.list_id = l.id AND lm.deleted_at IS NULL
    ) as list_last_update,
    (
        SELECT COUNT(*) FROM list_map lm WHERE lm.list_id = l.id AND lm.deleted_at IS NULL
    ) as list_count,
	array(
        SELECT
            DISTINCT unnest(a.tags)
        FROM anime a
        JOIN list_map lm ON a.id = lm.anime_id
        WHERE
            lm.list_id = l.id AND
            a.deleted_at IS NULL AND
            lm.deleted_at IS NULL
    ) as list_tags,
    array(
        SELECT
            DISTINCT unnest(a.languages)
        FROM anime a
        JOIN list_map lm ON a.id = lm.anime_id
        WHERE
            lm.list_id = l.id AND
            a.deleted_at IS NULL AND
            lm.deleted_at IS NULL
    ) as list_languages,
    array(
        SELECT
            DISTINCT unnest(a.language_types)
        FROM anime a
        JOIN list_map lm ON a.id = lm.anime_id
        WHERE
            lm.list_id = l.id AND
            a.deleted_at IS NULL AND
            lm.deleted_at IS NULL
    ) as list_language_types,
    array(
        SELECT
            DISTINCT a.type
        FROM anime a
        JOIN list_map lm ON a.id = lm.anime_id
        WHERE
            lm.list_id = l.id AND
            a.deleted_at IS NULL AND
            lm.deleted_at IS NULL
    ) as list_video_types,
    array(
        SELECT
            DISTINCT a.platform_id
        FROM anime a
        JOIN list_map lm ON a.id = lm.anime_id
        WHERE
            lm.list_id = l.id AND
            a.deleted_at IS NULL AND
            lm.deleted_at IS NULL
    ) as list_platforms,
    p.id as profile_id,
    p.username as profile_username,
    p.avatar as profile_avatar
FROM lists l
JOIN profiles p on l.profile_id = p.id
WHERE
    l.is_public = true AND
    l.deleted_at IS NULL AND
    p.deleted_at IS NULL
LIMIT {size} OFFSET {offset};

SELECT 
	COUNT(DISTINCT l.id) as count 
FROM lists l
JOIN profiles p on l.profile_id = p.id
WHERE
    l.is_public = true AND
    l.deleted_at IS NULL AND
    p.deleted_at IS NULL ";

		using var con = await _sql.CreateConnection();
		using var reader = await con.QueryMultipleAsync(query);
		var results = await reader.ReadAsync<CompPublicList>();
		var count = await reader.ReadSingleAsync<long>();

		return (results.ToArray(), count);
	}
}
