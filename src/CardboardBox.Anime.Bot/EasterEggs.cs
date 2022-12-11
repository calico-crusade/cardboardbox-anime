using F23.StringSimilarity;
using System.Net;

namespace CardboardBox.Anime.Bot
{
	using Manga;
	using Manga.Providers;
	using Services;

	using Match = System.Text.RegularExpressions.Match;

	public class EasterEggs
	{
		private readonly DiscordSocketClient _client;
		private readonly IDiscordApiService _api;
		private readonly ILogger _logger;
		private readonly IMangaApiService _manga;

		public EasterEggs(
			DiscordSocketClient client,
			IDiscordApiService api,
			ILogger<EasterEggs> logger,
			IMangaApiService manga)
		{
			_client = client;
			_api = api;
			_logger = logger;
			_manga = manga;
		}

		public Task Setup()
		{
			_client.MessageReceived += _client_MessageReceived;
			return Task.CompletedTask;
		}

		private Task _client_MessageReceived(SocketMessage arg)
		{
			return Task.Run(() => HandOff(arg));
		}

		public async void HandOff(SocketMessage arg)
		{
			var reference = new MessageReference(arg.Id, arg.Channel.Id);
			if (arg.Author.IsBot) return;

			if (!arg.MentionedUsers.Any(t => t.Id == _client.CurrentUser.Id)) return;

			if (arg.Reference == null || !arg.Reference.MessageId.IsSpecified) return;

			var msg = await arg.Channel.GetMessageAsync(arg.Reference.MessageId.Value);

			if (arg.Content.ToLower().Contains("lookup"))
			{
				await HandleMangaLookup(msg, arg, reference);
				return;
			}

			await HandleEmotes(msg, arg, reference);
		}

		public string DetermineExtension(StickerFormatType type)
		{
			return type switch
			{
				StickerFormatType.Png => "png",
				_ => "webp",
			};
		}

		public string? DetermineUrl(IMessage msg)
		{
			var img = msg.Attachments.FirstOrDefault(t => t.ContentType.StartsWith("image"));
			if (img != null) return img.Url;

			var content = msg.Content;
			if (Uri.IsWellFormedUriString(content, UriKind.Absolute)) return msg.Content;

			return null;
		}

		public async Task HandleMangaLookup(IMessage msg, SocketMessage rpl, MessageReference refe)
		{
			if (rpl.Channel is not SocketGuildChannel guild)
			{
				await msg.Channel.SendMessageAsync("This can only be used within servers.", messageReference: refe);
				return;
			}

			var settings = await _api.Settings(guild.Guild.Id);
			if (settings == null || !settings.EnableLookup)
			{
				await msg.Channel.SendMessageAsync("This feature is not enabled within this server. Contact Cardboard to get it enabled!", messageReference: refe);
				return;
			}

			var img = DetermineUrl(msg);
			if (img == null)
			{
				await msg.Channel.SendMessageAsync("I don't see any image attachments or URLs on the tagged message.", messageReference: refe);
				return;

			}

			var mod = await msg.Channel.SendMessageAsync("<a:loading:1048471999065903244> Processing your request...", messageReference: refe);

			try
			{
				var search = await _manga.Search(img);
				if (search == null || !search.Success)
				{
					await mod.ModifyAsync(t => t.Content = "I couldn't find any results that matched that image :(");
					return;
				}

				var embeds = new List<Embed>();

				var header = new EmbedBuilder()
					.WithAuthor(msg.Author)
					.WithTitle("Manga Search Results")
					.WithDescription("Here is what I found: ")
					.WithThumbnailUrl(img)
					.WithFooter("Cardboard Box | Manga")
					.WithCurrentTimestamp();

				foreach(var res in search.Vision)
					header.AddField(res.Title, $"Google Result: [{res.FilteredTitle}]({res.Url}) - (CF: {res.Score}, EM: {res.ExactMatch})");

				int count = 0;
				foreach(var res in search.Match)
				{
					if (count >= 5) break;

					if (res.Manga == null || res.Metadata == null || res.Score < 70) continue;

					header.AddField(res.Manga.Title, $"CBA Fallback: [Mangadex]({res.Manga.Url}) - (CF: {res.Score}, EM: {res.ExactMatch})");
					count++;
				}

				embeds.Add(header.Build());

				if (search.BestGuess != null)
					embeds.Add(new EmbedBuilder()
						.WithTitle(search.BestGuess.Title)
						.WithUrl(search.BestGuess.Url)
						.WithDescription(search.BestGuess.Description)
						.WithThumbnailUrl(search.BestGuess.Cover)
						.AddField("Tags", string.Join(", ", search.BestGuess.Tags))
						.AddField("Source", $"[{search.BestGuess.Source}]({search.BestGuess.Url})", true)
						.AddField("NSFW", search.BestGuess.Nsfw ? "yes" : "no", true)
						.WithFooter("Cardboard Box | Manga")
						.WithCurrentTimestamp()
						.Build());

				await mod.ModifyAsync(t =>
				{
					t.Embeds = embeds.ToArray();
					t.Content = "Here you go:";
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error occurred during manga lookup: {img}");
				await mod.ModifyAsync(t =>
				{
					t.Content = "Something went wrong! Contact Cardboard for more assistance or try again later!\r\n" +
					"Error Message: " + ex.Message;
				});
			}
		}

		public async Task HandleEmotes(IMessage msg, SocketMessage rpl, MessageReference refe)
		{
			var bob = new StringBuilder();

			foreach (var sticker in msg.Stickers)
				bob.AppendLine($"https://media.discordapp.net/stickers/{sticker.Id}.{DetermineExtension(sticker.Format)}");

			var emoteRegex = new Regex("<(a)?:(.*?):([0-9]{1,})>");
			var matches = emoteRegex.Matches(msg.Content);

			foreach(Match match in matches)
			{
				var isAnimated = match.Groups[1].Value == "a";
				var name = match.Groups[2].Value;
				var id = match.Groups[3].Value;
				var ext = isAnimated ? "gif" : "png";
				var url = $"https://cdn.discordapp.com/emojis/{id}.{ext}";
				bob.AppendLine($"Emote: {name} - {url}");
			}

			var compiled = bob.ToString();
			if (string.IsNullOrEmpty(compiled)) return;

			if (rpl.Channel is not SocketGuildChannel guild)
			{
				await msg.Channel.SendMessageAsync("This can only be used within servers.", messageReference: refe);
				return;
			}

			var settings = await _api.Settings(guild.Guild.Id);
			if (settings == null || !settings.EnableTheft)
			{
				await msg.Channel.SendMessageAsync("This feature is not enabled within this server. Contact Cardboard to get it enabled!", messageReference: refe);
				return;
			}

			await rpl.Channel.SendMessageAsync(compiled, messageReference: refe);
		}
	}
}
