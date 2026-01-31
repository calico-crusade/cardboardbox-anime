using MangaDexSharp;

namespace CardboardBox;

using Manga.MangaDex;

public static class DiExtensions
{
	public static IServiceCollection AddMangadex(this IServiceCollection services)
	{
		return services
			.AddMangaDex(c => c.WithApiConfig(userAgent: "cba-api"))
			.AddTransient<IMangaDexService, MangaDexService>();
	}
}
