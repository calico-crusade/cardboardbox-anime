namespace CardboardBox.Manga
{
	using Providers;

	public static class Extensions
	{
		public static IServiceCollection AddManga(this IServiceCollection services)
		{
			return services
				.AddTransient<IMangaService, MangaService>()
				
				.AddTransient<IMangakakalotSource, MangakakalotSource>();
		}
	}
}
