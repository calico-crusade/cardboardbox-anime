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
		private readonly Random _rnd = new();

		public HolybookCommands(
			IHolyBooksService holybooks, 
			ILogger<HolybookCommands> logger,
			IAnimeApiService api)
		{
			_holybooks = holybooks;
			_logger = logger;
			_api = api;
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

				var e = new EmbedBuilder()
					.WithTitle(anime.Title ?? "")
					.WithDescription(anime.Description ?? "")
					.WithColor(anime.PlatformColor())
					.WithImageUrl(anime.Images.FirstOrDefault(t => t.Type == "wallpaper")?.Source ?? anime.Images.OrderByDescending(t => t.Width).FirstOrDefault()?.Source ?? "")
					.WithCurrentTimestamp()
					.WithUrl(anime.Link)
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
