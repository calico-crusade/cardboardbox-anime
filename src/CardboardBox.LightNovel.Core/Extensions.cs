namespace CardboardBox.LightNovel.Core;

using Anime.Database;
using Anime.Database.Mapping;
using Database;
using Sources;
using Sources.Utilities;

public static class Extensions
{
	public static async IAsyncEnumerable<DbChapter> DbChapters(this ISourceService src, string firstUrl)
	{
		DbChapter? last = null;
		await foreach (var chapter in src.Chapters(firstUrl))
			yield return last = FromChapter(chapter, last);
	}

	public static DbChapter FromChapter(SourceChapter chapter, DbChapter? last)
	{
		return new DbChapter
		{
			HashId = chapter.Url.MD5Hash(),
			BookId = chapter.BookTitle.MD5Hash(),
			Book = chapter.BookTitle,
			Chapter = chapter.ChapterTitle,
			Content = chapter.Content,
			Url = chapter.Url,
			NextUrl = chapter.NextUrl,
			Ordinal = (last?.Ordinal ?? 0) + 1
		};
	}

	public static IServiceCollection AddLightNovel(this IServiceCollection services)
	{
		MapConfig.AddMap(c =>
		{
			c.ForEntity<Book>()
			 .ForEntity<Chapter>()
			 .ForEntity<Page>()
			 .ForEntity<Series>()
			 .ForEntity<ChapterPage>();
		});

		return services
			.AddTransient<ILnpSourceService, LnpSourceService>()
			.AddTransient<IShSourceService, ShSourceService>()
			.AddTransient<IReLibSourceService, ReLibSourceService>()

			.AddTransient<INovelUpdatesService, NovelUpdatesService>()

			.AddTransient<IOldLnApiService, OldLnApiService>()
			.AddTransient<INovelApiService, NovelApiService>()
			.AddTransient<INovelEpubService, NovelEpubService>()
			.AddTransient<IPdfService, PdfService>()
			
			.AddTransient<IDbBookService, DbBookService>()
			.AddTransient<IDbChapterService, DbChapterService>()
			.AddTransient<IDbPageService, DbPageService>()
			.AddTransient<IDbSeriesService, DbSeriesService>()
			.AddTransient<IDbChapterPageService, DbChapterPageService>()
			
			.AddTransient<ILnDbService, LnDbService>();
	}
}
