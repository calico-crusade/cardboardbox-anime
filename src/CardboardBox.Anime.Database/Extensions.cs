using Microsoft.Extensions.DependencyInjection;

namespace CardboardBox.Anime.Database
{
	using Mapping;
	using Generation;

	public static class Extensions
	{
		public static DbAnime Clean(this DbAnime anime)
		{
			var replaces = new Dictionary<string[], string[]>
			{
				[new[] { "sci-fi", "fantasy" }] = new[] { "sci fi and fantasy" },
				[new[] { "sci-fi", "action" }] = new[] { "science fiction - action" },
				[new[] { "sci-fi" }] = new[] { "sci fi", "animation - science fiction", "science fiction", "science fiction - comic" },
				[new[] { "action", "adventure" }] = new[] { "action and adventure", "action/adventure" },
				[new[] { "live-action" }] = new[] { "live action" },
				[new[] { "comedy" }] = new[] { "comedy – animation" },
				[new[] { "romance", "comedy" }] = new[] { "comedy – romance" },
				[new[] { "anime" }] = new[] { "animation - anime" },
				[new[] { "mystery", "thriller" }] = new[] { "mystery and thriller" },
				[new[] { "adventure" }] = new[] { "adventure - comic" },
				[new[] { "action" }] = new[] { "action - sword and sandal", "action - comic" }
			};

			var delParan = (string item) =>
			{
				if (!item.Contains('(')) return item.ToLower().Trim();
				return item.Split('(').First().Trim().ToLower();
			};

			var tagFix = (string item) =>
			{
				var value = item.ToLower().Trim();
				foreach (var (key, vals) in replaces)
					if (vals.Contains(value)) return key;
				return new[] { value };
			};

			anime.Languages = anime.Languages.Select(delParan).Distinct().ToArray();
			anime.Ratings = anime.Ratings.Select(t => t.ToLower().Trim().Split('|')).SelectMany(t => t).Distinct().ToArray();
			anime.Tags = anime.Tags.Select(tagFix).SelectMany(t => t).Distinct().ToArray();

			anime.Images = anime.Images.Select(t =>
			{
				if (t.Source.Contains("https:")) return t;
				t.Source = t.Source.Replace("https", "https:");
				return t;
			}).ToArray();

			return anime;
		}

		public static IEnumerable<DbAnime> Clean(this IEnumerable<DbAnime> anime)
		{
			foreach (var item in anime)
				yield return item.Clean();
		}

		public static IServiceCollection AddDatabase(this IServiceCollection services)
		{
			MapConfig.AddMap(c =>
			{
				c.ForEntity<DbAnime>()
				 .ForEntity<DbImage>()
				 .ForEntity<DbFilter>()
				 .ForEntity<DbProfile>()
				 .ForEntity<DbList>()
				 .ForEntity<DbListExt>()
				 .ForEntity<DbListMap>()
				 .ForEntity<DbListMapStripped>();
			});

			MapConfig.StartMap();

			return services
				.AddTransient<ISqlService, NpgsqlService>()
				.AddTransient<IDbQueryBuilderService, DbQueryBuilderService>()

				.AddTransient<IAnimeDbService, AnimeDbService>()
				.AddTransient<IProfileDbService, ProfileDbService>()
				.AddTransient<IListDbService, ListDbService>()
				.AddTransient<IListMapDbService, ListMapDbService>()
				
				.AddTransient<IDbService, DbService>();
		}
	}
}
