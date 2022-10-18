using CardboardBox.Anime.AI;
using CardboardBox.Anime.Bot;
using CardboardBox.Anime.Bot.Commands;
using CardboardBox.Anime.Holybooks;
using CardboardBox.Discord;
using Microsoft.Extensions.DependencyInjection;

await DiscordBotBuilder.Start()
	.WithServices(c =>
	{
		c.AddTransient<IHolyBooksService, HolyBooksService>()
		 .AddTransient<IAnimeApiService, AnimeApiService>()
		 .AddTransient<IAiAnimeService, AiAnimeService>();
	})
	.WithSlashCommands(c =>
	{
		c.With<HolybookCommands>();
	})
	.Build()
	.Login();

await Task.Delay(-1);