namespace CardboardBox.Anime.Bot;

public class LookupRequest : DbObject
{
	public string ImageUrl { get; set; } = string.Empty;

	public string MessageId { get; set; } = string.Empty;

	public string ChannelId { get; set; } = string.Empty;

	public string GuildId { get; set; } = string.Empty;

	public string AuthorId { get; set; } = string.Empty;

	public string ResponseId { get; set; } = string.Empty;

	public string? Results { get; set; }
}
