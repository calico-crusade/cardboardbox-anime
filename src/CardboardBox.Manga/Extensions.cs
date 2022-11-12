namespace CardboardBox.Manga
{
	using Providers;

	public static class Extensions
	{
		public static IServiceCollection AddManga(this IServiceCollection services)
		{
			return services
				.AddTransient<IMangaService, MangaService>()
				.AddTransient<IMangakakalotSource, MangakakalotSource>()
				.AddTransient<IMangaDexSource, MangaDexSource>()
				.AddTransient<IMangaEpubService, MangaEpubService>()
				.AddMangadex();
		}

		public static T? PreferedOrFirst<T>(this IEnumerable<T> data, Func<T, bool> prefered)
		{
			T? first = default;
			foreach(var item in data)
			{
				if (prefered(item)) return item;
				first ??= item;
			}

			return first;
		}
	}
}
