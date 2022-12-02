namespace CardboardBox.Anime.Database
{
	public class DbDiscordGuildSettings : DbObject
	{
		[JsonPropertyName("guildId")]
		public string GuildId { get; set; } = string.Empty;

		[JsonPropertyName("authedUsers")]
		public string[] AuthedUsers { get; set; } = Array.Empty<string>();

		[JsonPropertyName("enableLookup")]
		public bool EnableLookup { get; set; } = false;

		[JsonPropertyName("enableTheft")]
		public bool EnableTheft { get; set; } = false;

		[JsonPropertyName("mangaUpdatesChannel")]
		public string? MangaUpdatesChannel { get; set; }

		[JsonPropertyName("mangaUpdatesIds")]
		public string[] MangaUpdatesIds { get; set; } = Array.Empty<string>();

		[JsonPropertyName("mangaUpdatesNsfw")]
		public bool MangaUpdatesNsfw { get; set; } = false;
	}
}
