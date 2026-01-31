namespace CardboardBox.Anime.Core.MongoDb;

public interface IMongoConfig<T>
{
	string ConnectionString { get; }
	string DatabaseName { get; }
	string CollectionName { get; }
}

public class MongoConfig<T> : IMongoConfig<T>
{
	public string ConnectionString { get; set; } = "";
	public string DatabaseName { get; set; } = "";
	public string CollectionName { get; set; } = "";
}
