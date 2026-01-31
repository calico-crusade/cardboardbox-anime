namespace CardboardBox.Anime.Database;

using Core.Models;

public class DbAnime : DbObjectInt
{
	[JsonPropertyName("hashId")]
	public string HashId { get; set; } = "";

	[JsonPropertyName("animeId")]
	public string AnimeId { get; set; } = "";

	[JsonPropertyName("link")]
	public string Link { get; set; } = "";

	[JsonPropertyName("title")]
	public string Title { get; set; } = "";

	[JsonPropertyName("description")]
	public string Description { get; set; } = "";

	[JsonPropertyName("platformId")]
	public string PlatformId { get; set; } = "";

	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("mature")]
	public bool Mature { get; set; } = false;

	[JsonPropertyName("languages")]
	public string[] Languages { get; set; } = Array.Empty<string>();

	[JsonPropertyName("languageTypes")]
	public string[] LanguageTypes { get; set; } = Array.Empty<string>();

	[JsonPropertyName("ratings")]
	public string[] Ratings { get; set; } = Array.Empty<string>();

	[JsonPropertyName("tags")]
	public string[] Tags { get; set; } = Array.Empty<string>();

	[JsonPropertyName("images")]
	public DbImage[] Images { get; set; } = Array.Empty<DbImage>();

	[JsonPropertyName("ext")]
	public DbExtension[] Ext { get; set; } = Array.Empty<DbExtension>();

	[JsonPropertyName("otherPlatforms")]
	public List<DbAnime> OtherPlatforms { get; set; } = new();

	public static implicit operator DbAnime(Anime a)
	{
		return new DbAnime
		{
			HashId = a.HashId,
			AnimeId = a.AnimeId,
			Link = a.Link,
			Title = a.Title,
			Description = a.Description,
			PlatformId = a.PlatformId,
			Type = a.Type,
			Mature = a.Metadata.Mature,
			Languages = a.Metadata.Languages.ToArray(),
			LanguageTypes = a.Metadata.LanguageTypes.ToArray(),
			Ratings = a.Metadata.Ratings.ToArray(),
			Tags = a.Metadata.Tags.ToArray(),
			Images = a.Images.Select(t => (DbImage)t).ToArray(),
			Ext = a.Metadata.Ext.Select(t => new DbExtension { Type = t.Key, Value = t.Value }).ToArray(),
			CreatedAt = DateTime.Now,
			UpdatedAt = DateTime.Now
		};
	}
}