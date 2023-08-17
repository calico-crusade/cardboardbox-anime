using CardboardBox.Database;
using CardboardBox.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardboardBox.Anime;

using AI;
using Auth;
using ChatGPT;
using Crunchyroll;
using Core;
using Core.Models;
using Database;
using DiscordIntermediary;
using Funimation;
using HiDive;
using Manga;
using Vrv;

using LightNovel.Core;
using Json;

public static class Extensions
{
	public static IServiceCollection RegisterCba(this IServiceCollection services, IConfiguration config)
	{
		AnimeMongoService.RegisterMaps();

		return services
			.AddCardboardHttp()
			.AddJson()
			.AddLightNovel()
			.AddDatabase()
			.AddOAuth(config)
			.AddTransient<IFileCacheService, FileCacheService>()
			.AddTransient<IVrvApiService, VrvApiService>()
			.AddTransient<IFunimationApiService, FunimationApiService>()
			.AddTransient<IHiDiveApiService, HiDiveApiService>()
			.AddTransient<ICrunchyrollApiService, CrunchyrollApiService>()
			.AddMongo<Anime, AnimeConfig>()
			.AddTransient<IAnimeMongoService, AnimeMongoService>()
			.AddTransient<IAiAnimeService, AiAnimeService>()
			.AddTransient<IDiscordClient, DiscordClient>()
			.AddTransient<IChatGptService, ChatGptService>()
			.AddManga();
	}
}