namespace CardboardBox.Anime.Bot;

using ChatGPT;
using Services;

using Match = System.Text.RegularExpressions.Match;

public class EasterEggs
{
	private readonly DiscordSocketClient _client;
	private readonly IDiscordApiService _api;
	private readonly IMangaLookupService _lookup;
	private readonly IChatGptService _chat;
	private readonly ILogger _logger;
	private readonly IDbService _db;

	private static Dictionary<ulong, GptChat> _chats = new();
	private static ulong[] AUTHORIZED_USERS = { 191100926486904833 };

	public EasterEggs(
		DiscordSocketClient client,
		IDiscordApiService api,
		IMangaLookupService lookup,
		IChatGptService chat,
		ILogger<EasterEggs> logger,
		IDbService db)
	{
		_client = client;
		_api = api;
		_lookup = lookup;
		_chat = chat;
		_logger = logger;
		_db = db;
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

		if (arg.Channel is not SocketGuildChannel)
		{
			await HandleGpt(arg);
			return;
		}

		if (!arg.MentionedUsers.Any(t => t.Id == _client.CurrentUser.Id)) return;

		if (arg.Reference == null || !arg.Reference.MessageId.IsSpecified)
		{
			await HandleGpt(arg);
			return;
		}

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

	public async Task<bool> HandleEmotes(IMessage msg, SocketMessage rpl, MessageReference refe)
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
		if (string.IsNullOrEmpty(compiled)) return false;

		if (rpl.Channel is not SocketGuildChannel guild)
		{
			await msg.Channel.SendMessageAsync("This can only be used within servers.", messageReference: refe);
			return true;
		}

		var settings = await _api.Settings(guild.Guild.Id);
		if (settings == null || !settings.EnableTheft)
		{
			await msg.Channel.SendMessageAsync("This feature is not enabled within this server. Contact Cardboard to get it enabled!", messageReference: refe);
			return true;
		}

		await rpl.Channel.SendMessageAsync(compiled, messageReference: refe);
		return true;
	}

	public async Task HandleGpt(SocketMessage msg)
	{
		var reference = new MessageReference(msg.Id, msg.Channel.Id);
		if (msg.Author.IsBot || msg.Author.IsWebhook) return;

		ulong? guildId = msg.Channel is SocketGuildChannel ch ? ch.Guild.Id : null;
		if (!await _db.Gpt.ValidateUser(msg.Author.Id, guildId, AUTHORIZED_USERS))
		{
			_logger.LogInformation("User is not authorized");
			return;
		}

		var userId = msg.Author.Id;
		if (!_chats.ContainsKey(userId))
			_chats.Add(userId, new());

		var content = msg.Content.Replace($"<@{_client.CurrentUser.Id}>", "").Trim();

		var chat = _chats[userId];
		if (content.ToLower() == "clear")
		{
			chat.Messages = new();
			await msg.Channel.SendMessageAsync("Chat has been cleared.", messageReference: reference);
			return;
		}

		if (chat.Messages.Count == 0)
			chat.Messages.Add(GptMessage.System("You are talking to an idiot. Please speak very slowly and avoid big words."));


		chat.Messages.Add(GptMessage.User(content));

		var tokens = _chat.CountTokens(chat);
		if (tokens > 4096)
		{
			await msg.Channel.SendMessageAsync("Unforuntately, I can no longer talk to you. You've exceeded my idiocy limits. (Type `clear` to restart the conversation)", messageReference: reference);
			return;
		}

		var res = await msg.Channel.SendMessageAsync("<a:loading:1048471999065903244> Processing your request...", messageReference: reference);

		var resp = await _chat.Completions(chat);
		if (resp == null || resp.Choices.Length == 0)
		{
			await res.ModifyAsync(t => t.Content = "Unfortunately, something went wrong... Please try again later, or you can type `clear` to restart the conversation.");
			return;
		}

		var choice = resp.Choices.First();
		await res.ModifyAsync(t => t.Content = choice.Message.Content);
		chat.Messages.Add(GptMessage.Assistant(choice.Message.Content));

		_logger.LogInformation("[CHATGPT REPORT] Usage: Prompt {0} - Compeltion {1} - Total {2} >> {3} ({4}): \"{5}\"",
			resp.Usage.PromptTokens, resp.Usage.CompletionTokens, resp.Usage.TotalTokens,
			msg.Author.Username, msg.Author.Id, content);
	}
}
