namespace CardboardBox.LightNovel.Core;

using Anime.Database;

public class ChapterPage : DbObject
{
	[JsonPropertyName("chapterId")]
	public long ChapterId { get; set; }

	[JsonPropertyName("pageId")]
	public long PageId { get; set; }

	[JsonPropertyName("ordinal")]
	public long Ordinal { get; set; }
}
