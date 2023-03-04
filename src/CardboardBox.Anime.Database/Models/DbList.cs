namespace CardboardBox.Anime.Database;

public class DbList : DbObject
{
	[JsonPropertyName("title")]
	public string Title { get; set; } = "";

	[JsonPropertyName("description")]
	public string Description { get; set; } = "";

	[JsonPropertyName("profileId")]
	public long ProfileId { get; set; }

	[JsonPropertyName("isPublic")]
	public bool IsPublic { get; set; } = false;
}

public class DbListExt : DbList
{
	[JsonPropertyName("count")]
	public int Count { get; set; }

	[JsonPropertyName("counts")]
	public List<DbListCount> Counts { get; set; } = new();
}


public class DbListItem : DbObject
{
	[JsonPropertyName("listId")]
	public long ListId { get; set; }

	[JsonPropertyName("itemId")]
	public long ItemId { get; set; }

	[JsonPropertyName("type")]
	public ListItemType Type { get; set; }
}

public class DbListCount
{
	[JsonPropertyName("listId")]
	public long ListId { get; set; }

	[JsonPropertyName("type")]
	public ListItemType Type { get; set; }

	[JsonPropertyName("count")]
	public int Count { get; set; }
}

public enum ListItemType
{
	Anime = 1,
	Manga = 2,
	Novel = 3
}