namespace CardboardBox.Anime.Core.MongoDb;

public abstract class MongoEntity
{
	[BsonId]
	public string Id { get; set; } = "";
}
