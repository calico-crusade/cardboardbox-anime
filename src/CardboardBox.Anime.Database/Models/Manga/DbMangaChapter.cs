namespace CardboardBox.Anime.Database;

public class DbMangaChapter : DbObjectInt
{
	[JsonPropertyName("mangaId")]
	public long MangaId { get; set; } //

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty; //

	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty; //

	[JsonPropertyName("sourceId")]
	public string SourceId { get; set; } = string.Empty; //

	[JsonPropertyName("ordinal")]
	public double Ordinal { get; set; } //

	[JsonPropertyName("volume")]
	public double? Volume { get; set; } //

	[JsonPropertyName("language")]
	public string Language { get; set; } = "en"; //

	[JsonPropertyName("pages")]
	public string[] Pages { get; set; } = Array.Empty<string>();

	[JsonPropertyName("externalUrl")]
	public string? ExternalUrl { get; set; } //

	[JsonPropertyName("attributes")]
	public DbMangaAttribute[] Attributes { get; set; } = Array.Empty<DbMangaAttribute>(); //
}
