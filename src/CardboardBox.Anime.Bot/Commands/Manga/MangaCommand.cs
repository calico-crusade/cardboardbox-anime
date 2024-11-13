namespace CardboardBox.Anime.Bot.Commands;

using Services;

public class MangaCommand
{
	private readonly IComponentService _components;
	private readonly IMangaApiService _api;
	private readonly IMangaUtilityService _util;
	private readonly IDiscordApiService _settings;
	private readonly IMangaLookupService _lookup;

	private const ulong CARDBOARD_BOX = 1009959054073933885;

	public MangaCommand(
		IComponentService components,
		IMangaApiService api,
		IMangaUtilityService util,
		IDiscordApiService settings,
		IMangaLookupService lookup)
	{
		_components = components;
		_api = api;
		_util = util;
		_settings = settings;
		_lookup = lookup;
	}

	[Command("manga-image-lookup", "Search for a manga by an image", LongRunning = true)]
	public Task MangaImageLookup(SocketSlashCommand cmd,
		[Option("Image URL", true)] string url)
	{
		return _lookup.HandleLookup(cmd, url);
	}

	[Command("manga", "Search for a manga available on https://mangabox.app", LongRunning = true)]
	public async Task Manga(SocketSlashCommand cmd,
		[Option("Search Text", false)] string? search,
		[Option("Allow NSFW results", false)] bool? nsfw)
	{
		var filter = new MangaFilter
		{
			Search = search,
			Nsfw = (nsfw ?? false) ? NsfwCheck.DontCare : NsfwCheck.Sfw
		};
		var data = await _api.Search(filter);

		if (data == null || data.Count == 0 || data.Results.Length == 0)
		{
			await cmd.Modify("Couldn't find a manga that matches that search query!");
			return;
		}

		var manga = data.Results.First();
		var msg = await cmd.ModifyOriginalResponseAsync(f =>
		{
			f.Embed = _util.GenerateEmbed(manga)
				.Build();
			f.Content = "Search Text: " + search;
		});

		var comp = data.Count == 1 ?
			await _components.Components<MangaSearchReadComponent>(msg) :
			await _components.Components<MangaSearchComponent>(msg);
		await msg.ModifyAsync(f =>
		{
			f.Embed = _util.GenerateEmbed(manga)
				.AddOptField("Total Search Results", $"{1}/{data.Count}")
				.Build();
			f.Content = "Search Text: " + search;
			f.Components = comp;
		});
	}

	[GuildCommand("clear-settings-cache", "Clears the settings cache", CARDBOARD_BOX)]
	public async Task ClearCache(SocketSlashCommand cmd)
	{
		_settings.ClearCache();
		await cmd.Respond("Done", ephemeral: true);
	}
}