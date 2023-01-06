namespace CardboardBox.Anime.Bot
{
	using Services;

	using Match = System.Text.RegularExpressions.Match;

	public class EasterEggs
	{
		private readonly DiscordSocketClient _client;
		private readonly IDiscordApiService _api;
		private readonly ILogger _logger;
		private readonly IMangaApiService _manga;
		private readonly IDbService _db;
		private readonly IMangaLookupService _lookup;

		public EasterEggs(
			DiscordSocketClient client,
			IDiscordApiService api,
			ILogger<EasterEggs> logger,
			IMangaApiService manga,
			IDbService db,
			IMangaLookupService lookup)
		{
			_client = client;
			_api = api;
			_logger = logger;
			_manga = manga;
			_db = db;
			_lookup = lookup;
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

			var words = new[] { "lookup", "yo", "wat dis" };
			if (words.Any(t => arg.Content.ToLower().Contains(t)))
			{
				await _lookup.HandleLookup(msg, arg, reference);
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
