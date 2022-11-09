namespace CardboardBox.Anime.Core.Models
{
	public class MangaProgressPost
	{
		[JsonPropertyName("mangaId")]
		public long MangaId { get; set; }

		[JsonPropertyName("mangaChapterId")]
		public long MangaChapterId { get; set; }

		[JsonPropertyName("page")]
		public int Page { get; set; }
	}
}
