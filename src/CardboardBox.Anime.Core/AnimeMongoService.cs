namespace CardboardBox.Anime.Core;

using Models;

public class AnimeConfig : IMongoConfig<Anime>
{
	private readonly IConfiguration _config;

	public AnimeConfig(IConfiguration config)
	{
		_config = config;
	}

	public string ConnectionString => _config["Mongo:ConnectionString"];

	public string DatabaseName => "cba";

	public string CollectionName => "anime";
}

public interface IAnimeMongoService
{
	Task Upsert(Anime anime);
	Task Upsert(IEnumerable<Anime> anime);
	Task<PaginatedResult<Anime>> All(int page = 1, int size = 20, bool asc = true);
	Task<PaginatedResult<Anime>> All(AnimeFilter search);

	Task<Filter[]?> Filters();

	Task RegisterIndexes();
}

public class AnimeMongoService : IAnimeMongoService
{
	private readonly IMongoService<Anime> _mongo;
	private readonly ILogger _logger;

	private CacheItem<Filter[]> _filterCache;

	public IMongoCollection<Anime> Collection => _mongo.Collection;

	public FilterDefinitionBuilder<Anime> Filter => _mongo.Filter;

	public AnimeMongoService(
		IMongoService<Anime> mongo, 
		ILogger<AnimeMongoService> logger)
	{
		_mongo = mongo;
		_filterCache = new(RawFilters, 60);
		_logger = logger;
	}

	public Task Upsert(Anime anime)
	{
		return Collection.ReplaceOneAsync(Filter.Eq(t => t.HashId, anime.HashId), anime, new ReplaceOptions
		{
			IsUpsert = true
		});
	}

	public Task Upsert(IEnumerable<Anime> anime)
	{
		var ops = anime.Select(t => new ReplaceOneModel<Anime>(Filter.Eq(a => a.HashId, t.HashId), t)
		{
			IsUpsert = true
		}).ToList();

		return Collection.BulkWriteAsync(ops);
	}

	public Task<PaginatedResult<Anime>> All(int page = 1, int size = 20, bool asc = true)
	{
		return _mongo.Paginate(page, size, x => x.Title, asc);
	}

	public Task<Filter[]?> Filters() => _filterCache.Get();

	public Task<PaginatedResult<Anime>> All(AnimeFilter search)
	{
		var (page, size, text, langs, types, plats, tags, videoTypes, asc, mature) = search;
		var filters = new List<FilterDefinition<Anime>>();

		if (!string.IsNullOrEmpty(text))
			filters.Add(Filter.Text(text));

		if (langs.Any())
			filters.Add(Filter.AnyIn(t => t.Metadata.Languages, langs));

		if (types.Any())
			filters.Add(Filter.AnyIn(t => t.Metadata.LanguageTypes, types));

		if (plats.Any())
			filters.Add(Filter.In(t => t.PlatformId, plats));

		if (tags.Any())
			filters.Add(Filter.AnyIn(t => t.Metadata.Tags, tags));

		if (videoTypes.Any())
			filters.Add(Filter.In(t => t.Type, videoTypes));

		if (mature != AnimeFilter.MatureType.Both)
			filters.Add(Filter.Eq(t => t.Metadata.Mature, AnimeFilter.MatureType.Mature == mature));

		var filter = filters.Any() ? Filter.And(filters.ToArray()) : Filter.Empty;
		return _mongo.Paginate(page, size, t => t.Title, asc, filter);
	}

	public async Task<Filter[]> RawFilters()
	{
		return await new Dictionary<string, string>()
		{
			["languages"] = "metadata.languages",
			["types"] = "metadata.language_types",
			["video types"] = "type",
			["tags"] = "metadata.tags",
			["platforms"] = "platform_id"
		}.Select(async t =>
		{
			var (key, field) = t;
			var records = await Collection.DistinctAsync<string>(field, Filter.Empty).ToList();

			return new Filter(key, records.OrderBy(t => t).ToArray());
		}).WhenAll();
	}

	public async Task RegisterIndexes()
	{
		var indexes = new[]
		{
			Builders<Anime>.IndexKeys.Text(t => t.Title)
		}.Select(t => new CreateIndexModel<Anime>(t))
		.ToArray();

		var res = await Collection.Indexes.CreateManyAsync(indexes);
		_logger.LogInformation("Index set results: " + string.Join("\r\n", res));
	}

	public static void RegisterMaps()
	{
		BsonClassMap.RegisterClassMap<Anime>(c => c.AutoMap());
		BsonClassMap.RegisterClassMap<Image>(c => c.AutoMap());
		BsonClassMap.RegisterClassMap<Metadata>(c => c.AutoMap());
		BsonClassMap.RegisterClassMap<Season>(c => c.AutoMap());
	}
}

public record class Filter(string Key, string[] Values);
