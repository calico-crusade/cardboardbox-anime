using CardboardBox.Database;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace CardboardBox.Anime
{
	public static class Extensions
	{
		public static T Bind<T>(this IConfiguration config, string? section = null)
		{
			var i = Activator.CreateInstance<T>();
			var target = string.IsNullOrEmpty(section) ? config : config.GetSection(section);
			target.Bind(i);
			return i;
		}

		public static string MD5Hash(this string data)
		{
			using var md5 = MD5.Create();
			var input = Encoding.UTF8.GetBytes(data);
			var output = md5.ComputeHash(input);
			return Convert.ToHexString(output);
		}

		public static async Task<PaginatedResult<T>> Paginate<T>(
			this IMongoService<T> mongo,
			int page, int size,
			Expression<Func<T, object>> sort,
			bool ascending = true,
			FilterDefinition<T>? filter = null)
		{
			var countFacet = AggregateFacet.Create("count",
				PipelineDefinition<T, AggregateCountResult>.Create(new[]
				{
					PipelineStageDefinitionBuilder.Count<T>()
				}));

			var dataFacet = AggregateFacet.Create("data",
				PipelineDefinition<T, T>.Create(new[]
				{
					PipelineStageDefinitionBuilder.Sort(ascending ? Builders<T>.Sort.Ascending(sort) : Builders<T>.Sort.Descending(sort)),
					PipelineStageDefinitionBuilder.Skip<T>((page - 1) * size),
					PipelineStageDefinitionBuilder.Limit<T>(size)
				}));

			filter ??= mongo.Filter.Empty;
			var ag = (await mongo.Collection.Aggregate()
				.Match(filter)
				.Facet(countFacet, dataFacet)
				.ToListAsync()).First();

			var count = ag.Facets.First(t => t.Name == "count")
				.Output<AggregateCountResult>()?
				.FirstOrDefault()?
				.Count ?? 0;

			var total = (int)count / size;
			var data = ag.Facets.First(t => t.Name == "data").Output<T>();

			return new (total, count, data);
		}

		public static async Task<List<T>> ToList<T>(this Task<IAsyncCursor<T>> task)
		{
			return await (await task).ToListAsync();
		}

		public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
		{
			return Task.WhenAll(tasks);
		}
	}

	public record class PaginatedResult<T>(int Pages, long Count, IReadOnlyList<T> Results);
}