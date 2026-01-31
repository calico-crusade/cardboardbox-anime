namespace CardboardBox.Anime.Database;

public class DbMangaState : DbObjectInt
{
	[JsonPropertyName("messageId")]
	public ulong MessageId { get; set; }

	[JsonPropertyName("userId")]
	public ulong UserId { get; set; }

	[JsonPropertyName("guildId")]
	public ulong? GuildId { get; set; }

	[JsonPropertyName("channelId")]
	public ulong? ChannelId { get; set; }

	[JsonPropertyName("source")]
	public string Source { get; set; } = string.Empty;

	[JsonPropertyName("mangaId")]
	public long MangaId { get; set; }

	[JsonPropertyName("chapterId")]
	public long? ChapterId { get; set; }

	[JsonPropertyName("pageIndex")]
	public int? PageIndex { get; set; }
}
