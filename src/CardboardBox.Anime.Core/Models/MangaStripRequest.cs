namespace CardboardBox.Anime.Core
{
	public class MangaStripRequest
	{
		[JsonPropertyName("mangaId")]
		public long MangaId { get; set; }

		[JsonPropertyName("pages")]
		public Strip[] Pages { get; set; } = Array.Empty<Strip>();

		public MangaStripRequest() { }

		public MangaStripRequest(long mangaId, Strip[] pages)
		{
			MangaId = mangaId;
			Pages = pages;
		}

		public class Strip
		{
			[JsonPropertyName("chapterId")]
			public long ChapterId { get; set; }

			[JsonPropertyName("page")]
			public int Page { get; set; }

			public Strip() { }

			public Strip(long chapterId, int page)
			{
				ChapterId = chapterId;
				Page = page;
			}
		}
	}
}
