using CardboardBox.Anime.AI;
using CardboardBox.Anime.Bot;
using CardboardBox.Anime.Bot.Commands;
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
		 .AddTransient<IMangaUtilityService, MangaUtilityService>();
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

await Task.Delay(-1);