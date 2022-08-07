using Dapper;

namespace CardboardBox.Anime.Database
{
	using Core.Models;
	using Generation;

	public interface IAnimeDbService
	{
		Task<long> Upsert(DbAnime anime);
		Task<(int total, DbAnime[] results)> Search(FilterSearch search);
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
			var query = $@"SELECT * 
FROM anime 
WHERE {{0}} 
ORDER BY title {(search.Ascending ? "ASC" : "DESC")} 
LIMIT {search.Size} 
OFFSET {offset};";
			var count = @"SELECT COUNT(*) FROM anime WHERE {0};";

			var parts = new List<string>();
			var pars = new DynamicParameters();

			if (!string.IsNullOrEmpty(search.Search))
			{
				parts.Add("fts @@ phraseto_tsquery('english', :search)");
				pars.Add("search", search.Search);
			}

			if (search.Mature != FilterSearch.MatureType.Both)
			{
				parts.Add("mature = :mature");
				pars.Add("mature", search.Mature == FilterSearch.MatureType.Mature);
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

				parts.Add($"{key} && :{key}");
				pars.Add(key, vals);
			}

			foreach(var (key, vals) in ss)
			{
				if (!any(vals)) continue;
				parts.Add($"{key} = ANY( :{key} )");
				pars.Add(key, vals);
			}

			if (parts.Count == 0)
				parts.Add("1 = 1");

			var where = string.Join(" AND ", parts);
			var outputQuery = string.Format(query, where);
			var countQuery = string.Format(count, where);
			var fullQuery = $"{outputQuery}\r\n{countQuery}";

			using var con = _sql.CreateConnection();

			using var reader = await con.QueryMultipleAsync(fullQuery, pars);

			var results = (await reader.ReadAsync<DbAnime>()).ToArray();
			var total = await reader.ReadSingleAsync<int>();

			return (total, results);
		}
	}
}
