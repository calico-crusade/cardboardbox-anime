namespace CardboardBox.Anime.Bot;

using Services;

public class MangaUpdater
{
	private readonly IMangaApiService _api;
	private readonly ILogger _logger;
	private readonly IPersistenceService _persistence;
	private readonly IDiscordApiService _settings;
	private readonly DiscordSocketClient _client;
	private readonly IMangaUtilityService _util;

	public MangaUpdater(
		IMangaApiService api,
		ILogger<MangaUpdater> logger,
		IPersistenceService persistence,
		IDiscordApiService settings,
		DiscordSocketClient client,
		IMangaUtilityService util)
	{
		_api = api;
		_logger = logger;
		_persistence = persistence;
		_settings = settings;
		_client = client;
		_util = util;
	}

	public async Task Update()
	{
		try
		{
			_logger.LogInformation("Starting manga updater...");

			var res = await _api.Update(5);
			if (res == null || res.Length == 0)
			{
				_logger.LogWarning("No results for manga update");
				return;
			}

			foreach(var item in res)
				_logger.LogInformation($"Manga Updated: {item.Manga.Title} :: {item.Worked}");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while processing manga update");
		}
	}

	public async Task Channels()
	{
		try
		{
			await ChannelsDo();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred during channel updates");
		}
	}

	public async Task ChannelsDo()
	{
		var settings = await _persistence.Load();
		var last = settings.LastCheck ??= DateTime.UtcNow.AddHours(-5);

		var guilds = await _settings.Settings();
		if (guilds.Length == 0) return;

		var updateGuilds = guilds.Where(t => !string.IsNullOrEmpty(t.MangaUpdatesChannel)).ToArray();
		if (updateGuilds.Length == 0) return;

		var changes = await _api.Since(last) ?? new();

		if (changes.Results.Length == 0) return;

		foreach(var manga in changes.Results)
		foreach(var guild in guilds)
		{
			var embed = _util.GenerateShortEmbed(manga).Build();
			await ApplyChanges(embed, manga, guild);
		}
		
		settings.LastCheck = DateTime.UtcNow;
		await settings.Save();
	}

	public async Task ApplyChanges(Embed embed, MangaProgress progress, DbDiscordGuildSettings settings)
	{
		var manga = progress.Manga;
		//Verify that manga updates are turned on for this server
		if (string.IsNullOrEmpty(settings.MangaUpdatesChannel)) return;
		//Verify that NSFW manga updates are turned on for this server
		if (manga.Nsfw && !settings.MangaUpdatesNsfw) return;
		//Verify that the manga is in the selected manga update ids
		if (settings.MangaUpdatesIds.Length != 0 && !settings.MangaUpdatesIds.Contains(manga.Id.ToString())) return;
		//Validate guild and channel ids
		if (!ulong.TryParse(settings.GuildId, out var guildId) ||
			!ulong.TryParse(settings.MangaUpdatesChannel, out var channelId))
		{
			_logger.LogWarning($"Invalid ulong: {settings.Id}::\"{settings.GuildId}\" >> \"{settings.MangaUpdatesChannel}\"");
			return;
		}
		//Get the guild we're posting the message in
		var guild = _client.GetGuild(guildId);
		if (guild == null)
		{
			_logger.LogWarning($"Couldn't find guild: {settings.Id}::\"{settings.GuildId}\" >> \"{settings.MangaUpdatesChannel}\"");
			return;
		}
		//Get the channel we're posting the message in
		var channel = guild.GetChannel(channelId);
		if (channel == null || channel is not SocketTextChannel txt)
		{
			_logger.LogWarning($"Couldn't find channel: {settings.Id}::\"{settings.GuildId}\" >> \"{settings.MangaUpdatesChannel}\"");
			return;
		}
		//Post the embed
		await txt.SendMessageAsync(embed: embed);
	}
}
