using CardboardBox.Anime.AI;
using CardboardBox.Anime.Bot;
using CardboardBox.Anime.Bot.Commands;
using CardboardBox.Anime.Holybooks;
using CardboardBox.Discord;
using CardboardBox.Manga;
using Microsoft.Extensions.DependencyInjection;

var bot = DiscordBotBuilder.Start()
	.WithServices(c =>
	{
		c.AddTransient<IHolyBooksService, HolyBooksService>()
		 .AddTransient<IAnimeApiService, AnimeApiService>()
		 .AddTransient<IAiAnimeService, AiAnimeService>()
		 .AddManga();
	})
	.WithSlashCommands(c =>
	{
		c.With<HolybookCommands>();
		 //.With<MangaCommand>()
		 //.WithComponent<MangaComponent>();
	})
	.Build();

await bot.Login();

await Task.Delay(-1);