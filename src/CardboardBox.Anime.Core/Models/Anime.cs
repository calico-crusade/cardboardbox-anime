using MongoDB.Bson.Serialization.IdGenerators;

namespace CardboardBox.Anime.Core.Models
{
	[BsonIgnoreExtraElements]
	public class Anime
	{
		[JsonPropertyName("id")]
		[BsonId(IdGenerator = typeof(StringObjectIdGenerator)), BsonRepresentation(BsonType.ObjectId), BsonIgnoreIfDefault]
		public string? Id { get; set; }

		[JsonPropertyName("hashId")]
		[BsonElement("hash_id")]
		public string HashId { get; set; } = "";

		[JsonPropertyName("animeId")]
		[BsonElement("anime_id")]
		public string AnimeId { get; set; } = "";

		[JsonPropertyName("link")]
		[BsonElement("link")]
		public string Link { get; set; } = "";

		[JsonPropertyName("title")]
		[BsonElement("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("description")]
		[BsonElement("description")]
		public string Description { get; set; } = "";

		[JsonPropertyName("platformId")]
		[BsonElement("platform_id")]
		public string PlatformId { get; set; } = "";

		[JsonPropertyName("type")]
		[BsonElement("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("images")]
		[BsonElement("images")]
		public List<Image> Images { get; set; } = new();

		[JsonPropertyName("metadata")]
		[BsonElement("metadata")]
		public Metadata Metadata { get; set; } = new();
	}
}
