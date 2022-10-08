namespace CardboardBox.LightNovel.Core
{
	public class FullScaffold
	{
		[JsonPropertyName("series")]
		public Series Series { get; set; } = new();

		[JsonPropertyName("books")]
		public BookScaffold[] Books { get; set; } = Array.Empty<BookScaffold>();

		public class BookScaffold
		{
			[JsonPropertyName("book")]
			public Book Book { get; set; } = new();

			[JsonPropertyName("chapters")]
			public ChapterScaffold[] Chapters { get; set; } = Array.Empty<ChapterScaffold>();
		}

		public class ChapterScaffold
		{
			[JsonPropertyName("chapter")]
			public Chapter Chapter { get; set; } = new();

			[JsonPropertyName("pages")]
			public PageScaffold[] Pages { get; set; } = Array.Empty<PageScaffold>();
		}

		public class PageScaffold
		{
			[JsonPropertyName("page")]
			public Page Page { get; set; } = new();

			[JsonPropertyName("map")]
			public ChapterPage Map { get; set; } = new();
		}
	}
}
