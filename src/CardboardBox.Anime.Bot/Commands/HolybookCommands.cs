using CardboardBox.Anime.AI;
using CardboardBox.Anime.Holybooks;
using CardboardBox.Discord;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Maturity = CardboardBox.Anime.Core.Models.FilterSearch.MatureType;

namespace CardboardBox.Anime.Bot.Commands
{
	public class HolybookCommands
	{
		private readonly IHolyBooksService _holybooks;
		private readonly IAnimeApiService _api;
		private readonly ILogger _logger;
		private readonly IAiAnimeService _ai;
		private readonly Random _rnd = new();

		public HolybookCommands(
			IHolyBooksService holybooks, 
			ILogger<HolybookCommands> logger,
			IAnimeApiService api,
			IAiAnimeService ai)
		{
			_holybooks = holybooks;
			_logger = logger;
			_api = api;
			_ai = ai;
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

				var results = await _api.Random(new()
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

		//[Command("ai", "Generates an image with the given data", LongRunning = true)]
		public async Task Ai(SocketSlashCommand cmd,
			[Option("Generation Prompt", true)] string prompt,
			[Option("Negative Generation Prompt", false)] string? negativePrompt,
			[Option("Generation Steps (1 - 64)", false)] long? steps,
			[Option("CFG Scale (1 - 30)", false)] double? cfg,
			[Option("Generation Seed (1+)", false)] string? seed,
			[Option("Image Width (100 - 1024)", false)] long? width,
			[Option("Image Height (100 - 1024)", false)] long? height)
		{
			const long DEFAULT_STEPS = 20,
					  DEFAULT_SIZE = 512;
			const double DEFAULT_CFG = 7;
			const string DEFAULT_SEED = "-1";

			negativePrompt ??= "";
			steps ??= DEFAULT_STEPS;
			cfg ??= DEFAULT_CFG;
			seed ??= DEFAULT_SEED;
			width ??= DEFAULT_SIZE;
			height ??= DEFAULT_SIZE;

			if (width < 100 || width > 1024 || height < 100 || height > 1024)
			{
				await cmd.Modify("Image size has to be within 100x100 and 1024x1024!");
				return;
			}

			if (steps < 1 || steps > 64)
			{
				await cmd.Modify("Generation Steps has to be between 1 and 64");
				return;
			}

			if (cfg < 1 || cfg > 30)
			{
				await cmd.Modify("CFG has to be between 1 and 30");
				return;
			}

			if (!long.TryParse(seed, out long actualSeed) || (actualSeed < 1 && actualSeed != -1))
			{
				await cmd.Modify("Seed has to be a number and cannot be less than 1!");
				return;
			}

			var resp = await _ai.Get(new AiRequest
			{
				Prompt = prompt,
				NegativePrompt = negativePrompt,
				Steps = steps ?? DEFAULT_STEPS,
				CfgScale = cfg ?? DEFAULT_CFG,
				BatchCount = 1,
				BatchSize = 1,
				Seed = actualSeed,
				Width = width ?? DEFAULT_SIZE,
				Height = height ?? DEFAULT_SIZE
			});
			
			if (resp == null || resp.Images == null || resp.Images.Length == 0)
			{
				await cmd.Modify("Something went wrong! Contact an admin!");
				return;
			}

			var images = resp.Images.Select((t, i) =>
			{
				var temp = Path.GetRandomFileName() + ".png";
				var bytes = Convert.FromBase64String(t);
				File.WriteAllBytes(temp, bytes);

				var attach = new FileAttachment(temp);
				return (temp, attach);
			}).ToArray();
			
			await cmd.Modify("I have finished generating your image! Give me a second to post it! Thanks!");
			await cmd.Channel.SendFilesAsync(images.Select(t => t.attach));

			images.Each(t =>
			{
				File.Delete(t.temp);
			});
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
}
