namespace CardboardBox.Anime.Database
{
	public class DbList : DbObject
	{
		[JsonPropertyName("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("description")]
		public string Description { get; set; } = "";

		[JsonPropertyName("profileId")]
		public long ProfileId { get; set; }
	}

	public class DbListExt : DbList
	{
		[JsonPropertyName("count")]
		public int Count { get; set; }
	}
}
