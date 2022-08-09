namespace CardboardBox.Anime.Database
{
	public class DbListMap : DbObject
	{
		[JsonPropertyName("listId")]
		public long ListId { get; set; }

		[JsonPropertyName("animeId")]
		public long AnimeId { get; set; }
	}
}
