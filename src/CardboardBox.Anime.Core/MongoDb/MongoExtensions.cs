using System.Linq.Expressions;

namespace CardboardBox.Anime.Core.MongoDb;

public static class MongoDbExtensions
{
	private const string MONGO_PAG_COUNT_DEFAULT = "count";
	private const string MONGO_PAG_DATA_DEFAULT = "data";

	public static async Task<PaginatedResult<T>> Paginate<T>(
		this IMongoService<T> mongo,
		int page, int size,
		Expression<Func<T, object>> sort,
		bool ascending = true,
		FilterDefinition<T>? filter = null,
		string countName = MONGO_PAG_COUNT_DEFAULT,
		string dataName = MONGO_PAG_DATA_DEFAULT)
	{
		var countFacet = AggregateFacet.Create(countName,
			PipelineDefinition<T, AggregateCountResult>.Create(new[]
			{
					PipelineStageDefinitionBuilder.Count<T>()
			}));

		var dataFacet = AggregateFacet.Create(dataName,
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

		var count = ag.Facets.First(t => t.Name == countName)
			.Output<AggregateCountResult>()?
			.FirstOrDefault()?
			.Count ?? 0;

		var total = (int)count / size;
		var data = ag.Facets.First(t => t.Name == dataName).Output<T>();

		return new(total, (int)count, data.ToArray());
	}

	public static Task<T> Find<T>(this IMongoService<T> service, string id) where T : MongoEntity
	{
		return service.Collection.Find(t => t.Id == id).SingleOrDefaultAsync();
	}

	public static Task<List<T>> Find<T>(this IMongoService<T> service, Func<FilterDefinitionBuilder<T>, FilterDefinition<T>> action)
	{
		var filter = action(service.Filter);
		return service.Collection.Find(filter).ToListAsync();
	}

	public static Task Update<T>(this IMongoService<T> service, T entity) where T : MongoEntity
	{
		return service.Collection.ReplaceOneAsync(
			service.Filter.Eq(t => t.Id, entity.Id),
			entity);
	}

	public static Task<ReplaceOneResult> Upsert<T>(this IMongoService<T> service, T entity) where T : MongoEntity
	{
		return service.Collection.ReplaceOneAsync(
			service.Filter.Eq(t => t.Id, entity.Id),
			entity,
			new ReplaceOptions { IsUpsert = true });
	}

	public static Task Insert<T>(this IMongoService<T> service, T entity) where T : MongoEntity
	{
		return service.Collection.InsertOneAsync(entity);
	}

	public static async Task<List<T>> ToList<T>(this Task<IAsyncCursor<T>> task)
	{
		return await (await task).ToListAsync();
	}

	public static IServiceCollection AddMongo<T, TConfig>(this IServiceCollection services) where TConfig : class, IMongoConfig<T>
	{
		return services
			.AddTransient<IMongoConfig<T>, TConfig>()
			.AddTransient<IMongoService<T>, MongoService<T>>();
	}

	public static IServiceCollection AddMongo<T>(this IServiceCollection services, IMongoConfig<T> config)
	{
		return services
			.AddSingleton(config)
			.AddTransient<IMongoService<T>, MongoService<T>>();
	}

	public static IServiceCollection AddMongo<T>(this IServiceCollection services, string connectionString, string databaseName, string collectionName)
	{
		return AddMongo<T>(services, new MongoConfig<T>
		{
			ConnectionString = connectionString,
			DatabaseName = databaseName,
			CollectionName = collectionName
		});
	}
}
