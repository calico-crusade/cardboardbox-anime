using CardboardBox.Database;
using CardboardBox.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardboardBox.Anime
{
	using Auth;
	using Crunchyroll;
	using Core;
	using Core.Models;
	using Database;
	using Funimation;
	using HiDive;
	using Vrv;

	using LightNovel.Core;

	public static class Extensions
	{
		public static IServiceCollection RegisterCba(this IServiceCollection services, IConfiguration config)
		{
			AnimeMongoService.RegisterMaps();

			return services
				.AddCardboardHttp()
				.AddDatabase()
				.AddOAuth(config)
				.AddTransient<IVrvApiService, VrvApiService>()
				.AddTransient<IFunimationApiService, FunimationApiService>()
				.AddTransient<IHiDiveApiService, HiDiveApiService>()
				.AddTransient<ICrunchyrollApiService, CrunchyrollApiService>()
				.AddMongo<Anime, AnimeConfig>()
				.AddTransient<IAnimeMongoService, AnimeMongoService>()
				
				.AddTransient<ILightNovelApiService, LightNovelApiService>()
				.AddTransient<IPdfService, PdfService>();
		}
	}
}