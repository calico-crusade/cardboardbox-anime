using Dapper;

namespace CardboardBox.Anime.Database
{
	using Core;
	using Core.Models;
	using Generation;

	public interface IAnimeDbService
	{
		Task<long> Upsert(DbAnime anime);
		Task<(int total, DbAnime[] results)> Search(FilterSearch search);
		Task<DbAnime[]> All();
		Task<Filter[]> Filters();
	}

	public class AnimeDbService : OrmMapExtended<DbAnime>, IAnimeDbService
	{
		private string? _upsertQuery;

		public override string TableName => "anime";

		public AnimeDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<long> Upsert(DbAnime anime)
		{
			_upsertQuery ??= _query.Upsert<DbAnime, long>(TableName,
				(v) => v.With(t => t.HashId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertQuery, anime);
		}

		public async Task<(int total, DbAnime[] results)> Search(FilterSearch search)
		{
			int offset = (search.Page - 1) * search.Size;
			var query = $@"SELECT a.* 
FROM anime a
{{1}}
WHERE {{0}} 
ORDER BY a.title {(search.Ascending ? "ASC" : "DESC")} 
LIMIT {search.Size} 
OFFSET {offset};";
			var count = @"SELECT COUNT(*) FROM anime a {1} WHERE {0};";
			var sub = "";

			var parts = new List<string>();
			var pars = new DynamicParameters();

			if (!string.IsNullOrEmpty(search.Search))
			{
				parts.Add("a.fts @@ phraseto_tsquery('english', :search)");
				pars.Add("search", search.Search);
			}

			if (search.Mature != FilterSearch.MatureType.Both)
			{
				parts.Add("a.mature = :mature");
				pars.Add("mature", search.Mature == FilterSearch.MatureType.Mature);
			}

			if (search.ListId != null)
			{
				sub = @"JOIN list_map lm on a.id = lm.anime_id
JOIN lists l on lm.list_id = l.id";
				parts.Add("l.id = :listId");
				pars.Add("listId", search.ListId);
			}

			var queries = search.Queryables;
			var qs = new Dictionary<string, string[]?>
			{
				["languages"] = queries.Languages,
				["language_types"] = queries.Types,
				["tags"] = queries.Tags
			};

			var ss = new Dictionary<string, string[]?>
			{
				["platform_id"] = queries.Platforms,
				["type"] = queries.VideoTypes
			};

			var any = (string[]? ar) => ar?.Any() ?? false;
			foreach(var (key, vals) in qs)
			{
				if (!any(vals)) continue;

				parts.Add($"a.{key} && :{key}");
				pars.Add(key, vals);
			}

			foreach(var (key, vals) in ss)
			{
				if (!any(vals)) continue;
				parts.Add($"a.{key} = ANY( :{key} )");
				pars.Add(key, vals);
			}

			if (parts.Count == 0)
				parts.Add("1 = 1");

			var where = string.Join(" AND ", parts);
			var outputQuery = string.Format(query, where, sub);
			var countQuery = string.Format(count, where, sub);
			var fullQuery = $"{outputQuery}\r\n{countQuery}";

			using var con = _sql.CreateConnection();

			using var reader = await con.QueryMultipleAsync(fullQuery, pars);

			var results = (await reader.ReadAsync<DbAnime>()).ToArray();
			var total = await reader.ReadSingleAsync<int>();

			return (total, results);
		}

		public async Task<Filter[]> Filters()
		{
			const string QUERY = @"SELECT
    *
FROM (
    SELECT DISTINCT 'languages' as key, unnest(languages) as value FROM anime
    UNION
    SELECT DISTINCT 'types' as key, unnest(language_types) as value FROM anime
    UNION
    SELECT DISTINCT 'video types' as key, type as value from anime
    UNION
    SELECT DISTINCT 'tags' as key, unnest(tags) as value from anime
    UNION
    SELECT DISTINCT 'platforms' as key, platform_id as value from anime
) x
ORDER BY key, value";

			var filters = await _sql.Get<DbFilter>(QUERY);
			return filters
				.GroupBy(t => t.Key, t => t.Value)
				.Select(t => new Filter(t.Key, t.ToArray()))
				.ToArray();
		}
	}
}
