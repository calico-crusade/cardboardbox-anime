namespace CardboardBox.Anime.Database
{
	public class DbProfile : DbObject
	{
		[JsonPropertyName("username")]
		public string Username { get; set; } = "";

		[JsonPropertyName("avatar")]
		public string Avatar { get; set; } = "";

		[JsonPropertyName("platformId")]
		public string PlatformId { get; set; } = "";

		[JsonPropertyName("admin")]
		public bool Admin { get; set; } = false;

		[JsonIgnore]
		public string Email { get; set; } = "";
	}
}
