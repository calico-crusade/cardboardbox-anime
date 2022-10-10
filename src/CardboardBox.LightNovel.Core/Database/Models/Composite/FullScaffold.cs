namespace CardboardBox.LightNovel.Core
{
	public class FullScaffold
	{
		[JsonPropertyName("series")]
		public Series Series { get; set; } = new();

		[JsonPropertyName("books")]
		public BookScaffold[] Books { get; set; } = Array.Empty<BookScaffold>();

		public void Deconstruct(out Series series, out BookScaffold[] books)
		{
			series = Series;
			books = Books;
		}
	}

	public class FullBookScaffold : BookScaffold
	{
		[JsonPropertyName("series")]
		public Series Series { get; set; } = new();

		public void Deconstruct(out Series series, out Book book, out ChapterScaffold[] chaps)
		{
			series = Series;
			book = Book;
			chaps = Chapters;
		}
	}

	public class BookScaffold
	{
		[JsonPropertyName("book")]
		public Book Book { get; set; } = new();

		[JsonPropertyName("chapters")]
		public ChapterScaffold[] Chapters { get; set; } = Array.Empty<ChapterScaffold>();

		public void Deconstruct(out Book book, out ChapterScaffold[] chaps)
		{
			book = Book;
			chaps = Chapters;
		}
	}

	public class ChapterScaffold
	{
		[JsonPropertyName("chapter")]
		public Chapter Chapter { get; set; } = new();

		[JsonPropertyName("pages")]
		public PageScaffold[] Pages { get; set; } = Array.Empty<PageScaffold>();

		public void Deconstruct(out Chapter chapter, out PageScaffold[] pages)
		{
			chapter = Chapter;
			pages = Pages;
		}
	}

	public class PageScaffold
	{
		[JsonPropertyName("page")]
		public Page Page { get; set; } = new();

		[JsonPropertyName("map")]
		public ChapterPage Map { get; set; } = new();

		public void Deconstruct(out Page page, out ChapterPage map)
		{
			page = Page;
			map = Map;
		}
	}
}
