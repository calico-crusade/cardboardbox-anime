namespace CardboardBox.Anime.Core.Models
{
	[BsonIgnoreExtraElements]
	public class Season
	{
		[BsonElement("episode_count")]
		[JsonPropertyName("episodeCount")]
		public int EpisodeCount { get; set; }

		[BsonElement("type")]
		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[BsonElement("order")]
		[JsonPropertyName("order")]
		public int Order { get; set; }

		[BsonElement("number")]
		[JsonPropertyName("number")]
		public int Number { get; set; }

		[BsonElement("alt_title")]
		[JsonPropertyName("altTitle")]
		public string? AltTitle { get; set; }

		[BsonElement("season_id")]
		[JsonPropertyName("id")]
		public string Id { get; set; } = "";
	}
}
