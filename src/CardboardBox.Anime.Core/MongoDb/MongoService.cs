namespace CardboardBox.Anime.Core.MongoDb;

public interface IMongoBaseService
{
	IMongoClient Client { get; }

	IMongoClient GetClient();
}

public interface IMongoService<T> : IMongoBaseService
{
	IMongoDatabase Database { get; }
	IMongoCollection<T> Collection { get; }
	FilterDefinitionBuilder<T> Filter { get; }
}

public interface IMongoService : IMongoBaseService
{
	IMongoDatabase GetDatabase(string name);

	IMongoCollection<T> GetCollection<T>(string db, string name);
}

public class MongoService<T> : MongoService, IMongoService<T>
{
	private readonly IMongoConfig<T> _config;

	public string DatabaseName => _config.DatabaseName;

	public string CollectionName => _config.CollectionName;

	public IMongoDatabase Database => GetDatabase(DatabaseName);

	public IMongoCollection<T> Collection => GetCollection<T>(DatabaseName, CollectionName);

	public override string ConnectionString => _config.ConnectionString;

	public FilterDefinitionBuilder<T> Filter => Builders<T>.Filter;

	public MongoService(IMongoConfig<T> config)
	{
		_config = config;
	}
}

public abstract class MongoService : IMongoService
{
	private IMongoClient? _client;

	public abstract string ConnectionString { get; }

	public virtual IMongoClient Client => _client ??= GetClient();

	public virtual IMongoClient GetClient() => new MongoClient(ConnectionString);

	public virtual IMongoDatabase GetDatabase(string name) => Client.GetDatabase(name);

	public virtual IMongoCollection<T> GetCollection<T>(string db, string name) => GetDatabase(db).GetCollection<T>(name);
}
