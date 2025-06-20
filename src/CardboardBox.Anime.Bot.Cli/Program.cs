﻿using CardboardBox.Anime.AI;
using CardboardBox.Anime.Bot;
using CardboardBox.Anime.Bot.Commands;
using CardboardBox.Anime.Bot.Commands.Nsfw;
using CardboardBox.Anime.Bot.Commands.TierLists;
using CardboardBox.Anime.Bot.Services;
using CardboardBox.Anime.Holybooks;
using CardboardBox.Discord;
using CardboardBox.Http;
using CardboardBox.Json;
using CardboardBox.Manga;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

var isLocal = Environment.GetCommandLineArgs().Any(t => t.ToLower().Contains("local"));

var config = isLocal ? new DiscordSocketConfig
{
	UseInteractionSnowflakeDate = false,
	GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
} : new DiscordSocketConfig
{
	GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
};

var client = new DiscordSocketClient(config);
var bot = DiscordBotBuilder.Start(null, client)
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
		 .AddManga()
		 .AddCardboardHttp()
		 .AddJson();
	})
	.WithSlashCommands(c =>
	{
		c.With<HolybookCommands>()
		 .With<DateCommands>()
		 .With<DndCommands>()
		 .With<MangaCommand>()
		 .With<TierListCommands>()
		 .With<NsfwCommands>()
		 .With<GptToggleCommands>()
		 .With<AiCommands>()
		 .WithComponent<MangaSearchComponent>()
		 .WithComponent<MangaReadComponent>()
		 .WithComponent<MangaSearchReadComponent>()
		 .WithComponent<AiComponent>();
	})
	.Build();

await bot.Login();

if (!isLocal)
	bot.Background<MangaUpdater>(t => t.Channels(), out _, 60);
	   
bot.Background<EasterEggs>(t => t.Setup(), out _);

await Task.Delay(-1);