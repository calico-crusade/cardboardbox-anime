using CardboardBox.Anime.Bot.Services;

namespace CardboardBox.Anime.Bot
{
	public class EasterEggs
	{
		private readonly DiscordSocketClient _client;
		private readonly IGoogleVisionService _vision;
		private readonly IDiscordApiService _api;

		public EasterEggs(
			DiscordSocketClient client, 
			IGoogleVisionService vision,
			IDiscordApiService api)
		{
			_client = client;
			_vision = vision;
			_api = api;
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

			if (arg.Content.ToLower().Contains("https://ifunny.co/video/"))
				await HandleVideo(arg, reference);

			if (arg.Content.ToLower().Contains("https://ifunny.co/picture/"))
				await HandleImage(arg, reference);

			if (!arg.MentionedUsers.Any(t => t.Id == _client.CurrentUser.Id))
				return;

			if (arg.Reference == null || !arg.Reference.MessageId.IsSpecified)
				return;

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

			if (msg.Attachments.Count == 0)
			{
				await msg.Channel.SendMessageAsync("The tagged message has no attachments!", messageReference: refe);
				return;
			}

			var img = msg.Attachments.FirstOrDefault(t => t.ContentType.StartsWith("image"));
			if (img == null)
			{
				await msg.Channel.SendMessageAsync("I don't see any image attachments on the tagged message.", messageReference: refe);
				return;
			}

			var request = await _vision.ExecuteVisionRequest(img.Url);
			if (request == null)
			{
				await msg.Channel.SendMessageAsync("I couldn't find any matches for that image.", messageReference: refe);
				return;
			}

			var bob = new EmbedBuilder()
				.WithTitle("Manga Lookup Request")
				.WithDescription($"My best guess is: {request.Guess} ({request.Score * 100:0.00}%)")
				.WithImageUrl(img.Url)
				.WithCurrentTimestamp()
				.WithAuthor(msg.Author)
				.WithUrl("https://cba.index-0.com/manga");

			for (var i = 0; i < request.WebPages.Length && i < 5; i++)
			{
				var cur = request.WebPages[i];
				bob.AddField("Result #" + (i + 1), $"[{cur.Title}]({cur.Url})");
			}

			await msg.Channel.SendMessageAsync(embed: bob.Build(), messageReference: refe);
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

		public async Task HandleVideo(SocketMessage msg, MessageReference refe)
		{
			var regex = new Regex("https://ifunny.co/video/(.*?)?s=cl");
			var match = regex.Match(msg.Content);
			if (!match.Success) return;

			var url = match.Value;
			var html = new HtmlWeb();
			var doc = (await html.LoadFromWebAsync(url)).DocumentNode;

			var video = doc.SelectSingleNode("//video[@class='Fv7s']")?.Attributes["data-src"]?.Value;
			if (!string.IsNullOrEmpty(video))
				await msg.Channel.SendMessageAsync(video, messageReference: refe);
		}

		public async Task HandleImage(SocketMessage msg, MessageReference refe)
		{
			var regex = new Regex("https://ifunny.co/picture/(.*?)?s=cl");
			var match = regex.Match(msg.Content);
			if (!match.Success) return;

			var url = match.Value;
			var html = new HtmlWeb();
			var doc = (await html.LoadFromWebAsync(url)).DocumentNode;

			var img = doc.SelectSingleNode("//img[@class='f+2d']")?.Attributes["src"]?.Value;
			if (!string.IsNullOrEmpty(img))
				await msg.Channel.SendMessageAsync(img, messageReference: refe);
		}
	}
}
