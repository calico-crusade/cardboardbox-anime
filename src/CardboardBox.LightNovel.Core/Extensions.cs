namespace CardboardBox.LightNovel.Core
{
	using Anime.Database.Mapping;
	using Database;
	using Sources;

	public static class Extensions
	{
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
}
