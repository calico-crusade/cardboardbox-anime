namespace CardboardBox.Anime.Bot
{
	public class EasterEggs
	{
		private readonly DiscordSocketClient _client;

		public EasterEggs(DiscordSocketClient client)
		{
			_client = client;
		}

		public Task Setup()
		{
			_client.MessageReceived += _client_MessageReceived;
			return Task.CompletedTask;
		}

		private async Task _client_MessageReceived(SocketMessage arg)
		{
			var reference = new MessageReference(arg.Id, arg.Channel.Id);
			if (arg.Author.IsBot) return;

			if (arg.Content.ToLower().Contains("https://ifunny.co/video/"))
				await HandleVideo(arg, reference);

			if (arg.Content.ToLower().Contains("https://ifunny.co/picture/"))
				await HandleImage(arg, reference);

			if (!arg.MentionedUsers.Any(t => t.Id == _client.CurrentUser.Id))
				return;

			if (arg.Reference != null && arg.Reference.MessageId.IsSpecified)
			{
				var msg = await arg.Channel.GetMessageAsync(arg.Reference.MessageId.Value);
				await HandleEmotes(msg, arg, reference);
			}
		}

		public string DetermineExtension(StickerFormatType type)
		{
			return type switch
			{
				StickerFormatType.Png => "png",
				_ => "webp",
			};
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
