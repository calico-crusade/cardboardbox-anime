using CardboardBox.Anime.AI;
using CardboardBox.Anime.Bot;
using CardboardBox.Anime.Bot.Commands;
using CardboardBox.Anime.Bot.Services;
using CardboardBox.Anime.Holybooks;
using CardboardBox.Discord;
using Microsoft.Extensions.DependencyInjection;

var bot = DiscordBotBuilder.Start()
	.WithServices(c =>
	{
		c.AddTransient<IHolyBooksService, HolyBooksService>()
		 .AddTransient<IAnimeApiService, AnimeApiService>()
		 .AddTransient<IAiAnimeService, AiAnimeService>()
		 .AddTransient<IMangaApiService, MangaApiService>()
		 .AddTransient<IMangaUtilityService, MangaUtilityService>()
		 .AddTransient<IGoogleVisionService, GoogleVisionService>()
		 .AddSingleton<IDiscordApiService, DiscordApiService>()
		 .AddTransient<IPersistenceService, PersistenceService>()
		 .AddTransient<MangaUpdater>()
		 .AddTransient<EasterEggs>();
	})
	.WithSlashCommands(c =>
	{
		c.With<HolybookCommands>()
		 .With<MangaCommand>()
		 .WithComponent<MangaSearchComponent>()
		 .WithComponent<MangaReadComponent>()
		 .WithComponent<MangaSearchReadComponent>();
	})
	.Build();

await bot.Login();

bot.Background<MangaUpdater>(t => t.Update(), out _, 60 * 5)
   .Background<MangaUpdater>(t => t.Channels(), out _, 60)
   .Background<EasterEggs>(t => t.Setup(), out _);

await Task.Delay(-1);