using Microsoft.Extensions.DependencyInjection;

namespace CardboardBox.Anime.Database
{
	using Mapping;
	using Generation;

	public static class Extensions
	{
		public static IServiceCollection AddDatabase(this IServiceCollection services)
		{
			MapConfig.AddMap(c =>
			{
				c.ForEntity<DbAnime>()
				 .ForEntity<DbImage>();
			});

			MapConfig.StartMap();

			return services
				.AddTransient<ISqlService, NpgsqlService>()
				.AddTransient<IDbQueryBuilderService, DbQueryBuilderService>()
				.AddTransient<IAnimeDbService, AnimeDbService>();
		}
	}
}
