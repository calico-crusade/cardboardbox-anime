
using DiscordClient = Discord.WebSocket.DiscordSocketClient;

namespace CardboardBox.Anime.Bot.Commands;

using Holybooks;
using Services;
using Maturity = AnimeFilter.MatureType;

public class HolybookCommands
{
	public const long DEFAULT_STEPS = 20, DEFAULT_SIZE = 512;
	public const double DEFAULT_CFG = 7, DEFAULT_DENOISE = 0.7;
	public const string DEFAULT_SEED = "-1";
	public const ulong CARDBOARD_BOX_SERVER = 1009959054073933885;
	public const ulong CARDBOARD_BOX_ROLE = 1067468243532533831;

	private readonly IApiService _api;
	private readonly IHolyBooksService _holybooks;
	private readonly IAnimeApiService _anime;
	private readonly ILogger _logger;
	private readonly DiscordClient _client;
	private readonly Random _rnd = new();

	public HolybookCommands(
		IHolyBooksService holybooks, 
		ILogger<HolybookCommands> logger,
		IAnimeApiService anime,
		IApiService api,
		DiscordClient client)
	{
		_holybooks = holybooks;
		_logger = logger;
		_anime = anime;
		_api = api;
		_client = client;
	}

	[Command("ping", "Checks to see if the bot is still alive.")]
	public Task Ping(SocketSlashCommand cmd) => cmd.RespondAsync("Pong", ephemeral: true);

	[Command("holybook", "Fetches one of the holy books.", LongRunning = true)]
	public async Task Holybook(SocketSlashCommand cmd, [Option("The language of the book", false)] string? language)
	{
		await HandleBook(cmd, language);
	}

	[Command("anime", "Search for an anime", LongRunning = true)]
	public async Task Anime(SocketSlashCommand cmd,
		[Option("Search text", false)] string? search,
		[Option("Maturity", false, "Everyone", "Mature")] string? maturity,
		[Option("Audio Language", false, "Chinese", "English", "Japanese", "Portuguese", "Spanish")] string? language,
		[Option("Platform", false, "Crunchyroll", "Funimation", "Hidive", "Mondo", "Vrv Select")] string? platform,
		[Option("Audio Type", false, "Dubbed", "Subbed", "Unknown")] string? audio,
		[Option("Video Type", false, "Movie", "Series")] string? type)
	{
		try
		{
			var toList = (string? input, bool lower) => {
				if (string.IsNullOrEmpty(input)) return null;
				if (lower) input = input.ToLower();
				return new[] { input.Replace(" ", "") };
			};

			var results = await _anime.Random(new()
			{
				Page = 1,
				Size = 3000,
				Search = search,
				Ascending = true,
				Mature = maturity.Case(Maturity.Both, ("Everyone", Maturity.Everyone), ("Mature", Maturity.Mature)),
				Queryables = new()
				{
					Languages = toList(language, true),
					Platforms = toList(platform, true),
					Types = toList(audio, false),
					VideoTypes = toList(type, true)
				}
			});

			if (results == null || results.Length == 0)
			{
				await cmd.Modify("No anime found for those search results.");
				return;
			}
			var anime = results.First();

			var platforms = new[] { anime }
				.Union(anime.OtherPlatforms)
				.Select(t => $"[{t.PlatformId}]({t.Link})");

			var platImgs = new Dictionary<string, string>
			{
				["mondo"] = "https://cdn.discordapp.com/attachments/1009959055026028556/1011774163238789190/mondo-icon.png",
				["funimation"] = "https://cdn.discordapp.com/attachments/1009959055026028556/1011774164270596187/funimation-icon.png",
				["vrvselect"] = "https://cdn.discordapp.com/attachments/1009959055026028556/1011774163536597012/vrvselect-icon.png",
				["crunchyroll"] = "https://cdn.discordapp.com/attachments/1009959055026028556/1011774163926659152/crunchyroll-icon.png",
				["hidive"] = "https://cdn.discordapp.com/attachments/1009959055026028556/1011774164593545276/hidive-icon.png"
			};

			var e = new EmbedBuilder()
				.WithTitle(anime.Title ?? "")
				.WithDescription(anime.Description ?? "")
				.WithColor(anime.PlatformColor())
				.WithImageUrl(anime.Images.FirstOrDefault(t => t.Type == "wallpaper")?.Source ?? anime.Images.OrderByDescending(t => t.Width).FirstOrDefault()?.Source ?? "")
				.WithCurrentTimestamp()
				.WithUrl(anime.Link)
				.WithThumbnailUrl(platImgs[anime.PlatformId])
				.WithFooter("CardboardBox")
				.AddOptField("Genres", string.Join(", ", anime.Tags), true)
				.AddOptField("Languages", string.Join(", ", anime.Languages), true)
				.AddOptField("Video Type", string.Join(", ", anime.LanguageTypes), true)
				.AddOptField("Type", anime.Type ?? "", true)
				.AddOptField("Platform(s)", string.Join(", ", platforms), true);

			if (anime.Mature)
				e.AddField("Mature", "Yes", true);

			await cmd.Modify(e);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while requesting anime");
			await cmd.Modify("An error occurred.");
		}
	}

	[GuildCommand("guilds", "Displays a list of discord servers the bot is in", CARDBOARD_BOX_SERVER)]
	public async Task Servers(SocketSlashCommand cmd,
		[Option("Name of the server to get the invite link for", false)] string? name)
	{
		try
		{
			if (string.IsNullOrEmpty(name))
			{
				var names = string.Join(", ", _client.Guilds.Select(t => t.Name));
				await cmd.RespondAsync("Discord Guilds: " + names, ephemeral: true);
				return;
			}

			var guild = _client.Guilds.FirstOrDefault(t => t.Name.ToLower() == name.ToLower());
			if (guild == null)
			{
				await cmd.RespondAsync("Couldn't find a guild with that name", ephemeral: true);
				return;
			}

			var link = await guild.GetVanityInviteAsync();
			if (link == null)
				link = (await guild.GetInvitesAsync())?.FirstOrDefault(t => !t.IsTemporary);

			if (link == null)
			{
				await cmd.RespondAsync("Couldn't get an invite link for said server", ephemeral: true);
				return;
			}

			await cmd.RespondAsync("Invite link: " + link.Url, ephemeral: true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while procressing servers request");
			await cmd.RespondAsync("Error occurred while processing your request", ephemeral: true);
		}
	}

	[Command("secret", "Exposes all of your inner-most secrets!", LongRunning = true)]
	public async Task Secrets(SocketSlashCommand cmd,
		[Option("Secret Type", false, "fetish")] string? type,
		[Option("Who to expose", false)] IUser? user)
	{
		static string OffsetForCardboard(string[] items, Random rnd)
		{
			var index = (rnd.Next(items.Length) - 1).Martial(items.Length);
			return items[index];
		}

		var uid = user?.Id ?? cmd.User.Id;
        var userRan = new Random(CalculateSeed(uid));
		var validTypes = new[] { "gif", "png", "jpg", "jpeg", "webp" };
		(string name, string[] path)[] types = new[]
		{
			("fetish", new[] { "ImageSets", "Fetish" })
		};

		var path = types.PreferedOrFirst(t => t.name.ToLower().Trim() == type?.ToLower().Trim()).path;

		var files = Directory.GetFiles(Path.Combine(path))
			.Where(t => validTypes.Contains(Path.GetExtension(t).Trim('.').ToLower()))
			.OrderBy(t => t)
			.ToArray();

		if (files.Length == 0)
		{
			await cmd.Modify("Unfortunately, I was unable to handle your request at this time. Please try again later!");
			return;
		}

		var image = OffsetForCardboard(files, userRan);
		await cmd.ModifyOriginalResponseAsync(c =>
		{
			c.Content = $"Here is <@{uid}>'s deepest darkest secret: ";
			c.Attachments = new FileAttachment[]
			{
				new FileAttachment(image)
			};
			c.AllowedMentions = new AllowedMentions(AllowedMentionTypes.None);
		});
	}

	private int CalculateSeed(ulong id)
	{
        return int.Parse(new string(id.ToString().TakeLast(6).ToArray()));
    }

	private async Task<(bool, string)> GetImage(string url)
	{
		var (stream, length, file, type) = await _api.GetData(url);
		if (stream == null)
			return (false, "Image failed to download!");

		if (!type.ToLower().StartsWith("image"))
			return (false, "Return result needs to be an image! Mimetype received: " + type);

		byte[] bytes = Array.Empty<byte>();
		using (var io = new MemoryStream())
		{
			await stream.CopyToAsync(io);
			io.Position = 0;
			bytes = io.ToArray();
			await stream.DisposeAsync();
		}

		return (true, Convert.ToBase64String(bytes));
	}

	private async Task HandleBook(SocketSlashCommand cmd, string? language, int error = 0)
	{
		if (error >= 3)
		{
			await cmd.Modify("Error occurred while fetching a specific image.");
			return;
		}

		try
		{
			var languages = await _holybooks.GetLanguages();
			Language? lang = null;

			if (!string.IsNullOrEmpty(language))
				lang = languages?.FirstOrDefault(t => t?.Name?.ToLower()?.Trim() == language.ToLower().Trim());

			if (lang == null) lang = languages?[_rnd.Next(0, languages.Length)];

			if (lang == null || string.IsNullOrEmpty(lang?.Path))
			{
				await cmd.Modify("Error occurred while fetching available languages");
				return;
			}

			var files = await _holybooks.GetFiles(lang.Path);
			var file = files?[_rnd.Next(0, files.Length)];
			if (file == null || string.IsNullOrEmpty(file?.DownloadUrl))
			{
				await HandleBook(cmd, language, error + 1);
				return;
			}

			await cmd.Modify(file.DownloadUrl);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while handling holybook: " + language);
			await HandleBook(cmd, language, error + 1);
		}
	}
}
