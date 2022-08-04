using CardboardBox.Database;
using CardboardBox.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CardboardBox.Anime
{
	using Core;
	using Core.Models;
	using Funimation;
	using Vrv;

	public static class Extensions
	{
		public static IServiceCollection RegisterCba(this IServiceCollection services)
		{
			AnimeMongoService.RegisterMaps();

			return services
				.AddCardboardHttp()
				.AddTransient<IVrvApiService, VrvApiService>()
				.AddTransient<IFunimationApiService, FunimationApiService>()
				.AddMongo<Anime, AnimeConfig>()
				.AddTransient<IAnimeMongoService, AnimeMongoService>();
		}
	}
}