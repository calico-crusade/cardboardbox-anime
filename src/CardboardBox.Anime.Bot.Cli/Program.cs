using CardboardBox.Anime.AI;
using CardboardBox.Anime.Bot;
using CardboardBox.Anime.Bot.Commands;
using CardboardBox.Anime.Bot.Commands.Nsfw;
using CardboardBox.Anime.Bot.Services;
using CardboardBox.Anime.Holybooks;
using CardboardBox.Discord;
using CardboardBox.Manga;
using Microsoft.Extensions.DependencyInjection;

var isLocal = Environment.GetCommandLineArgs().Any(t => t.ToLower().Contains("local"));

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
		 .AddTransient<IMangaLookupService, MangaLookupService>()
		 .AddDatabase()
		 .AddTransient<MangaUpdater>()
		 .AddTransient<EasterEggs>()
		 .AddManga();
	})
	.WithSlashCommands(c =>
	{
		c.With<HolybookCommands>()
		 .With<MangaCommand>()
		 .With<NsfwCommands>()
		 .With<GptToggleCommands>()
		 .WithComponent<MangaSearchComponent>()
		 .WithComponent<MangaReadComponent>()
		 .WithComponent<MangaSearchReadComponent>();
	})
	.Build();

await bot.Login();

if (!isLocal)
	bot.Background<MangaUpdater>(t => t.Channels(), out _, 60);
	   
bot.Background<EasterEggs>(t => t.Setup(), out _);

await Task.Delay(-1);