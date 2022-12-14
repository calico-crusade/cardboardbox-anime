namespace CardboardBox.Manga
{
	using Match;
	using Providers;

	public static class Extensions
	{
		public static IServiceCollection AddManga(this IServiceCollection services)
		{
			return services
				.AddTransient<IMangaService, MangaService>()

				.AddTransient<IMangakakalotTvSource, MangakakalotTvSource>()
				.AddTransient<IMangakakalotComSource, MangakakalotComSource>()
				.AddTransient<IMangakakalotComAltSource, MangakakalotComAltSource>()
				.AddTransient<IMangaDexSource, MangaDexSource>()
				.AddTransient<IMangaClashSource, MangaClashSource>()
				.AddTransient<INhentaiSource, NhentaiSource>()
				.AddTransient<IBattwoSource, BattwoSource>()

				.AddTransient<IMangaEpubService, MangaEpubService>()
				.AddTransient<IMangaImageService, MangaImageService>()

				.AddTransient<IMatchApiService, MatchApiService>()
				.AddTransient<IMangaMatchService, MangaMatchService>()

				.AddTransient<IGoogleVisionService, GoogleVisionService>()
				.AddTransient<IMangaSearchService, MangaSearchService>()

				.AddMangadex();
		}

		public static T? PreferedOrFirst<T>(this IEnumerable<T> data, Func<T, bool> prefered)
		{
			foreach(var item in data)
			{
				if (prefered(item)) return item;
			}

			return data.FirstOrDefault();
		}

		public static IEnumerable<T[]> Split<T>(this IEnumerable<T> data, int count)
		{
			var total = (int)Math.Ceiling((decimal)data.Count() / count);
			var current = new List<T>();

			foreach(var item in data)
			{
				current.Add(item);

				if (current.Count == total)
				{
					yield return current.ToArray();
					current.Clear();
				}
			}

			if (current.Count > 0) yield return current.ToArray();
		}
	}
}
