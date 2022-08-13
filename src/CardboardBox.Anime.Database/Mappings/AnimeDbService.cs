using Dapper;

namespace CardboardBox.Anime.Database
{
	using Core;
	using Core.Models;
	using Generation;

	public interface IAnimeDbService
	{
		Task<long> Upsert(DbAnime anime);
		Task<(int total, DbAnime[] results)> Search(FilterSearch search, string? platformId = null);
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

		public async Task<(int total, DbAnime[] results)> Search(FilterSearch search, string? platformId = null)
		{
			int offset = (search.Page - 1) * search.Size;
			var query = $@"CREATE TEMP TABLE titles AS
SELECT 
	DISTINCT a.title
FROM anime a
{{1}}
WHERE {{0}}
ORDER BY a.title {(search.Ascending ? "ASC" : "DESC")} 
LIMIT {search.Size} 
OFFSET {offset};

SELECT
	DISTINCT
	a.*
FROM anime a
JOIN titles t ON t.title = a.title
ORDER BY a.title {(search.Ascending ? "ASC" : "DESC")}, a.platform_id ASC;

SELECT COUNT(DISTINCT a.title) FROM anime a {{1}} WHERE {{0}};

DROP TABLE titles;";
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
JOIN lists l on lm.list_id = l.id
JOIN profiles p ON p.id = l.profile_id";
				parts.Add("l.id = :listId");
				parts.Add("l.deleted_at IS NULL");
				parts.Add("p.deleted_at IS NULL");
				parts.Add("lm.deleted_at IS NULL");
				parts.Add(@"(p.platform_id = :pPlatformId OR l.is_public = true)");
				pars.Add("listId", search.ListId);
				pars.Add("pPlatformId", platformId);
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

			parts.Add("a.deleted_at IS NULL");

			var where = string.Join(" AND ", parts);
			var fullQuery = string.Format(query, where, sub);

			using var con = _sql.CreateConnection();

			using var reader = await con.QueryMultipleAsync(fullQuery, pars);

			var results = (await reader.ReadAsync<DbAnime>()).ToArray();
			var total = await reader.ReadSingleAsync<int>();

			return (total, GroupPlatforms(results).ToArray());
		}

		public IEnumerable<DbAnime> GroupPlatforms(IEnumerable<DbAnime> anime)
		{
			DbAnime? previous = null;
			foreach(var item in anime)
			{
				if (previous == null)
				{
					previous = item;
					continue;
				}

				if (previous.Title != item.Title)
				{
					yield return previous;
					previous = item;
					continue;
				}

				previous.OtherPlatforms.Add(item);
			}

			if (previous != null) yield return previous;
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
