namespace CardboardBox.LightNovel.Core
{
	using Database;

	public interface ILnDbService
	{
		IDbBookService Books { get; }
		IDbChapterService Chapters { get; }
		IDbPageService Pages { get; }
		IDbSeriesService Series { get; }
		IDbChapterPageService ChapterPages { get; }
	}

	public class LnDbService : ILnDbService
	{
		public IDbBookService Books { get; }
		public IDbChapterService Chapters { get; }
		public IDbPageService Pages { get; }
		public IDbSeriesService Series { get; }
		public IDbChapterPageService ChapterPages { get; }

		public LnDbService(
			IDbBookService books, 
			IDbChapterService chapters,
			IDbPageService pages,
			IDbSeriesService series,
			IDbChapterPageService chapterPages)
		{
			Books = books;
			Chapters = chapters;
			Pages = pages;
			Series = series;
			ChapterPages = chapterPages;
		}
	}
}
