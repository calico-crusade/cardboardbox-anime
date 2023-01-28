namespace CardboardBox.Anime.Database;

public class DbMangaAttribute
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("value")]
	public string Value { get; set; } = string.Empty;

	public DbMangaAttribute() { }

	public DbMangaAttribute(string name, string value)
	{
		Name = name;
		Value = value;
	}
}
