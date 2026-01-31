namespace CardboardBox.Anime.Database;

public class DbDiscordLog : DbObjectInt
{
    public string MessageId { get; set; } = string.Empty;

    public string AuthorId { get; set; } = string.Empty;

    public string? ChannelId { get; set; }

    public string? GuildId { get; set; }

    public string? ThreadId { get; set; }
    
    public string? ReferenceId { get; set; }

    public DateTime SendTimestamp { get; set; }

    public DbDiscordAttachment[] Attachments { get; set; } = Array.Empty<DbDiscordAttachment>();

    public string[] MentionedChannels { get; set; } = Array.Empty<string>();

    public string[] MentionedRoles { get; set; } = Array.Empty<string>();

    public string[] MentionedUsers { get; set; } = Array.Empty<string>();

    public DbDiscordSticker[] Stickers { get; set; } = Array.Empty<DbDiscordSticker>();

    public string? Content { get; set; }

    public int MessageType { get; set; }

    public int MessageSource { get; set; }
}

public class DbDiscordSticker
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Type { get; set; }

    public int Format { get; set; }

    public string Url { get; set; } = string.Empty;
}

public class DbDiscordAttachment
{
    public string Id { get; set; } = string.Empty;

    public string Filename { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}