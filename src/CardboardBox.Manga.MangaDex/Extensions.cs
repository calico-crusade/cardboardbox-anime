namespace CardboardBox;

using Manga.MangaDex;

public static class Extensions
{
	public static IServiceCollection AddMangadex(this IServiceCollection services)
	{
		return services.AddTransient<IMangaDexService, MangaDexService>();
	}
}
