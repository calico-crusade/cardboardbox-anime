namespace CardboardBox.Anime.Bot.Services;

public interface IMangaLookupService
{
	Task HandleLookup(IMessage msg, SocketMessage rpl, MessageReference refe);

	Task HandleEmojiLookup(IMessage msg, IMessageChannel channel, SocketReaction reaction);
}

public class MangaLookupService : IMangaLookupService
{
	private readonly IDiscordApiService _api;
	private readonly ILogger _logger;
	private readonly IMangaApiService _manga;
	private readonly IDbService _db;

	public const string IMPORT_URL = Constants.MANGA_UI + "/import?url=";

	public MangaLookupService(
		IDiscordApiService api, 
		ILogger<MangaLookupService> logger, 
		IMangaApiService manga, 
		IDbService db)
	{
		_api = api;
		_logger = logger;
		_manga = manga;
		_db = db;
	}

	public async Task HandleEmojiLookup(IMessage msg, IMessageChannel channel, SocketReaction reaction)
	{
		var img = DetermineUrl(msg);
		if (img == null) return;

		if (reaction.Channel is not SocketGuildChannel guild) return;

		var settings = await _api.Settings(guild.Guild.Id);
		if (settings == null || !settings.EnableLookup) return;

		string messageId = msg.Id.ToString(),
			guildId = guild.Guild.Id.ToString(),
			channelId = guild.Id.ToString(),
			authorId = reaction.UserId.ToString();

		var existing = await _db.Lookup.Fetch(messageId);
		if (existing != null && !string.IsNullOrEmpty(existing.Results))
		{
			await HandleIdiots(guild, msg, authorId, existing);
			return;
		}

		var men = AllowedMentions.None;
		men.MentionRepliedUser = false;

		var rpl = new MessageReference(msg.Id, channel.Id, guild.Guild.Id);

		var mod = await msg.Channel.SendMessageAsync(
			$"<@{authorId}> <a:loading:1048471999065903244> Processing your request...", 
			allowedMentions: men,
			messageReference: rpl);

		existing = new LookupRequest
		{
			MessageId = messageId,
			GuildId = guildId,
			ChannelId = channelId,
			AuthorId = authorId,
			ImageUrl = img,
			Results = null,
			ResponseId = mod.Id.ToString(),
		};
		existing.Id = await _db.Lookup.Upsert(existing);

		try
		{
			await DoSearch(mod, img, existing);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Error occurred during manga lookup: {img}");
			await mod.ModifyAsync(t =>
			{
				t.Content = "Something went wrong! " +
					"Contact Cardboard for more assistance or try again later!\r\n" +
					"Error Message: " + ex.Message;
			});
		}
	}

	public async Task HandleLookup(IMessage msg, SocketMessage rpl, MessageReference refe)
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

		string messageId = msg.Id.ToString(),
			   guildId = guild.Guild.Id.ToString(),
			   channelId = guild.Id.ToString(),
			   authorId = rpl.Author.Id.ToString();

		var existing = await _db.Lookup.Fetch(messageId);
		if (existing != null && !string.IsNullOrEmpty(existing.Results))
		{
			await HandleIdiots(guild, msg, authorId, existing);
			return;
		}

		var mod = await msg.Channel.SendMessageAsync("<a:loading:1048471999065903244> Processing your request...", messageReference: refe);

		existing = new LookupRequest
		{
			MessageId = messageId,
			GuildId = guildId,
			ChannelId = channelId,
			AuthorId = authorId,
			ImageUrl = img,
			Results = null,
			ResponseId = mod.Id.ToString(),
		};
		existing.Id = await _db.Lookup.Upsert(existing);

		try
		{
			await DoSearch(mod, img, existing);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, $"Error occurred during manga lookup: {img}");
			await mod.ModifyAsync(t =>
			{
				t.Content = "Something went wrong! " +
					"Contact Cardboard for more assistance or try again later!\r\n" +
					"Error Message: " + ex.Message;
			});
		}
	}

	public async Task HandleIdiots(SocketGuildChannel channel, IMessage msg, string authorId, LookupRequest request)
	{
		var refe = new MessageReference(ulong.Parse(request.ResponseId), channel.Id, channel.Guild.Id);
		await msg.Channel.SendMessageAsync($"Uh, <@{authorId}>, it's right here...", messageReference: refe);
	}

	public async Task DoSearch(IUserMessage msg, string imgUrl, LookupRequest data)
	{
		var search = await _manga.Search(imgUrl) ?? new ImageSearchResults();
		data.Results = Serialize(search);
		await _db.Lookup.Upsert(data);
		if (search == null || !search.Success)
		{
			await msg.ModifyAsync(t => t.Content = $"<@{data.AuthorId}> I couldn't find any results that matched that image :(");
			return;
		}

		var fallback = search.Match
			.OrderByDescending(t => t.Score)
			.FirstOrDefault();
		if (fallback != null && fallback.Manga != null && 
			(fallback.Score > 90 || fallback.ExactMatch))
		{
			await PrintFallback(msg, fallback, data);
			return;
		}

		await PrintOld(msg, imgUrl, search, data);
	}

	public string Encode(string url) => WebUtility.UrlEncode(url);

	public async Task PrintFallback(IUserMessage msg, FallbackResult result, LookupRequest data)
	{
		if (result.Manga == null) return;

		var embed = new EmbedBuilder()
			.WithTitle(result.Manga.Title)
			.WithUrl(result.Manga.Url)
			.WithThumbnailUrl(result.Manga.Cover)
			.WithDescription($"{result.Manga.Description}. [Reader]({IMPORT_URL}{Encode(result.Manga.Url)})")
			.AddField("Tags", string.Join(", ", result.Manga.Tags))
			.AddField("Score", $"{result.Score:0.00}. (EM: {result.ExactMatch})", true);

		if (result.Manga.Nsfw)
			embed.AddField("NSFW", "yes", true);

		if (result.Metadata != null)
		{
			embed.AddField("Source", $"[{result.Metadata.Source}]({result.Metadata.Url})", true);

			if (result.Metadata.Type == MangaMetadataType.Page && result.Metadata.Source == "mangadex")
				embed.AddField("Type", $"[Page](https://mangadex.org/chapter/{result.Metadata.ChapterId}/{result.Metadata.Page})", true);
			
			if (result.Metadata.Type == MangaMetadataType.Cover)
				embed.AddField("Type", "Cover", true);
		}

		await msg.ModifyAsync(t =>
		{
			t.Embed = embed.Build();
			t.Content = $"<@{data.AuthorId}>, Here you go:";
		});
	}

	public async Task PrintOld(IUserMessage mod, string img, ImageSearchResults search, LookupRequest data)
	{
		var embeds = new List<Embed>();

		var header = new EmbedBuilder()
			.WithTitle("Manga Search Results")
			.WithDescription("Here is what I found: ")
			.WithThumbnailUrl(img)
			.WithFooter("Cardboard Box | Manga")
			.WithCurrentTimestamp();

		foreach (var res in search.Vision)
			header.AddField(res.Title, $"Google Result: [{res.FilteredTitle}]({res.Url}) - (CF: {res.Score:0.00}, EM: {res.ExactMatch})");

		int count = 0;
		foreach (var res in search.Match)
		{
			if (count >= 5) break;

			if (res.Manga == null || res.Metadata == null || res.Score < 70) continue;

			header.AddField(res.Manga.Title, $"CBA Fallback: [Mangadex]({res.Manga.Url}) - (CF: {res.Score:0.00}, EM: {res.ExactMatch})");
			count++;
		}

		embeds.Add(header.Build());

		if (search.BestGuess != null)
			embeds.Add(new EmbedBuilder()
				.WithTitle(search.BestGuess.Title)
				.WithUrl(search.BestGuess.Url)
				.WithDescription($"{search.BestGuess.Description}. [Reader]({IMPORT_URL}{Encode(search.BestGuess.Url)})")
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
			t.Content = $"<@{data.AuthorId}>, Here you go:";
		});
	}

	public string? DetermineUrl(IMessage msg)
	{
		var img = msg.Attachments.FirstOrDefault(t => t.ContentType.StartsWith("image"));
		if (img != null) return img.Url;

		var content = msg.Content;
		if (Uri.IsWellFormedUriString(content, UriKind.Absolute)) return msg.Content;

		return null;
	}

	public string Serialize<T>(T data) => JsonSerializer.Serialize(data);

	public T? Deserialize<T>(string result) => JsonSerializer.Deserialize<T>(result);
}
