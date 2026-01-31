namespace CardboardBox.Anime.Bot.Commands.TierLists;

/// <summary>
/// Represents a tier list stored in the database
/// </summary>
public class TierList : DbObjectInt
{
    /// <summary>
    /// An MD5 hash of the URL
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// The URL the tier list was fetched from
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// The title of the tier list
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The different tiers on the list
    /// </summary>
    public string[] Tiers { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The image urls in the list
    /// </summary>
    public string[] Images { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents a tier list instance associated with a specific message
/// </summary>
public class TierListInstance : DbObjectInt
{
    /// <summary>
    /// The ID of the tier 
    /// </summary>
    public long TierId { get; set; }

    /// <summary>
    /// The ID of the guild the message was sent in
    /// </summary>
    public string GuildId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the channel the message was sent in
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the message 
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who created the tier list
    /// </summary>
    public string CreatorId { get; set; } = string.Empty;

    /// <summary>
    /// The user's tier list ratings
    /// </summary>
    public TierListMap[] Map { get; set; } = Array.Empty<TierListMap>();
}

public class TierListMap
{
    [JsonPropertyName("imageIndex")]
    public int ImageIndex { get; set; }

    [JsonPropertyName("tiers")]
    public Dictionary<int, TierListVote[]> Tiers { get; set; } = new();
}

public class TierListVote
{
    public int TierIndex { get; set; }

    public string[] Voters { get; set; } = Array.Empty<string>();
}