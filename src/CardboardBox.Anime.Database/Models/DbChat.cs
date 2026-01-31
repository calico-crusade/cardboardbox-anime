namespace CardboardBox.Anime.Database;

public class DbChat : DbObjectInt
{
	[JsonPropertyName("profileId")]
	public long ProfileId { get; set; }

	[JsonPropertyName("status")]
	public DbChatStatus Status { get; set; } = DbChatStatus.OnGoing;

	[JsonPropertyName("grounder")]
	public string? Grounder { get; set; }
}

public class DbChatMessage : DbObjectInt
{
	[JsonPropertyName("chatId")]
	public long ChatId { get; set; }

	[JsonPropertyName("profileId")]
	public long? ProfileId { get; set; }

	[JsonPropertyName("type")]
	public DbMessageType Type { get; set; } = DbMessageType.User;

	[JsonPropertyName("content")]
	public string Content { get; set; } = string.Empty;

	[JsonPropertyName("imageId")]
	public long? ImageId { get; set; }
}

public class DbChatData
{
	[JsonPropertyName("chat")]
	public DbChat Chat { get; set; } = new();

	[JsonPropertyName("messages")]
	public DbChatMessage[] Messages { get; set; } = Array.Empty<DbChatMessage>();
}

public enum DbChatStatus
{
	OnGoing = 0,
	ClientFinished = 1,
	ReachedLimit = 2,
	ErrorOccurred = 3
}

public enum DbMessageType
{
	User = 0,
	Bot = 1,
	Image = 2
}
