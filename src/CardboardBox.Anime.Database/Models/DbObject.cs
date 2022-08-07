namespace CardboardBox.Anime.Database
{
	public abstract class DbObject
	{
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("createdAt")]
		public DateTime? CreatedAt { get; set; }

		[JsonPropertyName("updatedAt")]
		public DateTime? UpdatedAt { get; set; }

		[JsonPropertyName("deletedAt")]
		public DateTime? DeletedAt { get; set; }
	}
}
