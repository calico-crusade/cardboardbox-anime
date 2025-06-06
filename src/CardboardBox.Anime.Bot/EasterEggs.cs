﻿namespace CardboardBox.Anime.Bot;

using ChatGPT;
using Services;
using Scripting;
using Scripting.Tokening;

using Match = System.Text.RegularExpressions.Match;
using CMsg = Cacheable<IUserMessage, ulong>;
using CMMsg = Cacheable<IMessage, ulong>;
using CChn = Cacheable<IMessageChannel, ulong>;

public class EasterEggs
{
	private readonly string[] TWITTER_FIX_DOMAINS = new[] { "x.com", "twitter.com" };

	private readonly DiscordSocketClient _client;
	private readonly IDiscordApiService _api;
	private readonly IMangaLookupService _lookup;
	private readonly IChatGptService _chat;
	private readonly ILogger _logger;
	private readonly IDbService _db;
	private readonly IServiceProvider _provider;
	private readonly ITokenService _token;
	private readonly IAnimeApiService _anime;

	private static Dictionary<ulong, GptChat> _chats = new();
	private static ulong[] AUTHORIZED_USERS = { 191100926486904833 };

	public EasterEggs(
		DiscordSocketClient client,
		IDiscordApiService api,
		IMangaLookupService lookup,
		IChatGptService chat,
		ILogger<EasterEggs> logger,
		IDbService db,
		IServiceProvider provider,
		IAnimeApiService anime,
		ITokenService token)
	{
		_client = client;
		_api = api;
		_lookup = lookup;
		_chat = chat;
		_logger = logger;
		_db = db;
		_provider = provider;
		_token = token;
		_anime = anime;
	}

	public Task Setup()
	{
		_client.MessageReceived += _client_MessageReceived;
		_client.ReactionAdded += _client_ReactionAdded;
		_client.MessageUpdated += _client_MessageUpdated;
        _client.MessageDeleted += _client_MessageDeleted;
		return Task.CompletedTask;
	}

    private Task _client_MessageDeleted(CMMsg arg1, CChn arg2)
    {
		_ = Task.Run(async () =>
		{
			try
			{
				await _anime.DeleteMessage(arg1.Id.ToString());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling message deletion");
			}
		});
		return Task.CompletedTask;
    }

    private Task _client_ReactionAdded(CMsg message, CChn channel, SocketReaction reaction)
	{
		return Task.Run(() => HandOff(message, channel, reaction));
	}

	private Task _client_MessageReceived(SocketMessage arg)
    {
        _ = LogMessage(arg);
        return Task.Run(() => HandOff(arg));
	}

	private Task _client_MessageUpdated(CMMsg oldMsg, SocketMessage msg, ISocketMessageChannel channel)
	{
        _ = LogMessage(msg);
		return Task.CompletedTask;
    }

	public void HandOff(CMsg message, CChn channel, SocketReaction reaction)
	{
		//var emotes = new[] { "🍝", "🔍", "🔎" };
		//if (!reaction.User.IsSpecified || 
		//	reaction.User.Value.IsBot || 
		//	!emotes.Contains(reaction.Emote.Name)) return;

		//var msg = await message.GetOrDownloadAsync();
		//var chn = await channel.GetOrDownloadAsync();

		//await _lookup.HandleEmojiLookup(msg, chn, reaction);
	}

	public Task LogMessage(SocketMessage msg)
	{
        return Task.Run(async () =>
        {
            try
            {
                await _anime.PostMessage(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging message");
            }
        });
    }

	public async void HandOff(SocketMessage arg)
    {
        if (arg.Author.IsBot) return;

		var reference = new MessageReference(arg.Id, arg.Channel.Id);

		if (arg.Channel is not SocketGuildChannel)
		{
			await HandleGpt(arg);
			return;
		}

		if (!arg.MentionedUsers.Any(t => t.Id == _client.CurrentUser.Id))
		{
			await HandleTwitterFix(arg);
            return;
		}

		if (arg.Content.ToLower().Contains("exec"))
		{
			if (!AUTHORIZED_USERS.Contains(arg.Author.Id)) return;

            await HandleScript(arg);
            return;
        }

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

		chat.Messages.Add(GptMessage.User(content));

		var tokens = _chat.CountTokens(chat);
		if (tokens > 4096)
		{
			await msg.Channel.SendMessageAsync("Unfortunately, I can no longer talk to you. You've exceeded my idiocy limits. (You've reached the max number of words/tokens per conversation, as set by ChatGPT. Type `clear` to restart the conversation)", messageReference: reference);
			return;
		}

		var res = await msg.Channel.SendMessageAsync("<a:loading:1048471999065903244> Processing your request...", messageReference: reference);

		var resp = await _chat.Completions(chat);
		if (resp == null || resp.Choices.Length == 0)
		{
			await res.ModifyAsync(t => t.Content = "Unfortunately, something went wrong... Please try again later, or you can type `clear` to restart the conversation.");
			return;
		}

		_logger.LogInformation("[CHATGPT REPORT] Usage: Prompt {0} - Compeltion {1} - Total {2} >> {3} ({4}): \"{5}\"",
			resp.Usage.PromptTokens, resp.Usage.CompletionTokens, resp.Usage.TotalTokens,
			msg.Author.Username, msg.Author.Id, content);

		var choice = resp.Choices.First();
		var message = choice.Message.Content;
		chat.Messages.Add(GptMessage.Assistant(choice.Message.Content));

		if (message.Length <= 2000)
		{
			await res.ModifyAsync(t => t.Content = choice.Message.Content);
			return;
		}

		var messages = StringExtensions.Split(message);
		await res.ModifyAsync(t => t.Content = "My response is a bit long, so I'm going to post it below. Bare with me!");

		foreach(var m in messages)
			await msg.Channel.SendMessageAsync(m, messageReference: reference);
	}

	public async Task HandleScript(SocketMessage msg)
    {
        var reference = new MessageReference(msg.Id, msg.Channel.Id);
        var reply = (string message) => msg.Channel.SendMessageAsync(message, messageReference: reference);
		try
		{
            var engine = new ScriptEngineBuilder()
				.WithProvider(_provider)
				.WithGlobalMethods<IScriptMethods>()
				.WithVariable("msg", msg)
				.WithMethod("reply", reply)
				.Build();

			var config = new TokenParserConfig("```js", "```", "\\");
			var tokens = _token.ParseTokens(msg.Content, config).ToArray();
			if (tokens.Length == 0) return;

			foreach (var token in tokens)
            {
                _logger.LogDebug("Found content: {username} ({id}) [{messageId}]: \r\n{content}",
                    msg.Author.Username, msg.Author.Id, msg.Id, token.Content.Trim('\r', '\n'));
                engine.Execute(token.Content);
            }
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while processing script");
			await reply($"An error occurred while processing your script: \r\n```\r\n{ex}\r\n```");
		}
	}

	public async Task<bool> HandleTwitterFix(SocketMessage msg)
	{
		var refe = new MessageReference(msg.Id, msg.Channel.Id);

        var urlRegex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)");
		if (!urlRegex.IsMatch(msg.Content)) return false;

		var urls = urlRegex.Matches(msg.Content)
			.Select(t => new Uri(t.ToString()))
			.Where(t => TWITTER_FIX_DOMAINS.Contains(t.Host.ToLower()))
			.ToArray();

		if (urls.Length == 0 || msg.Channel is not SocketGuildChannel guild) return false;

        var settings = await _api.Settings(guild.Guild.Id);
		if (settings == null || !settings.EnableTwitterUrls) return true;

        var changed = string.Join("\r\n", urls.Select(t => $"https://vxtwitter.com/" + t.PathAndQuery.TrimStart('/')));

		var men = new AllowedMentions
		{
			MentionRepliedUser = false,
		};
		await msg.Channel.SendMessageAsync(changed, messageReference: refe, allowedMentions: men);
		return true;
	}
}
