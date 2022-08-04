using CardboardBox.Database;

namespace CardboardBox.Anime.Core
{
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
	}

	public class AnimeMongoService : IAnimeMongoService
	{
		private readonly IMongoService<Anime> _mongo;

		public IMongoCollection<Anime> Collection => _mongo.Collection;

		public FilterDefinitionBuilder<Anime> Filter => _mongo.Filter;

		public AnimeMongoService(IMongoService<Anime> mongo)
		{
			_mongo = mongo;
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

		public static void RegisterMaps()
		{
			BsonClassMap.RegisterClassMap<Anime>(c => c.AutoMap());
			BsonClassMap.RegisterClassMap<Image>(c => c.AutoMap());
			BsonClassMap.RegisterClassMap<Metadata>(c => c.AutoMap());
			BsonClassMap.RegisterClassMap<Season>(c => c.AutoMap());
		}
	}
}
