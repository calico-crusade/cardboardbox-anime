namespace CardboardBox.Anime.Bot.Services;

public interface IAnimeApiService
{
	Task<DbAnime[]> Random(AnimeFilter search);
	Task<PaginatedResult<DbAnime>> Search(AnimeFilter search);
	Task<Filter[]?> Filters();
	Task<DbDiscordLog?> PostMessage(DbDiscordLog log);
	Task<DbDiscordLog?> PostMessage(SocketMessage message);
}

public class AnimeApiService : IAnimeApiService
{
	private readonly IConfiguration _config;
	private readonly IApiService _api;

	public string ApiUrl => _config["CBA:Url"] ?? throw new ArgumentNullException("CBA:URL");

	public AnimeApiService(
		IConfiguration config,
		IApiService api)
	{
		_config = config;
		_api = api;
	}

	public async Task<DbAnime[]> Random(AnimeFilter search)
	{
		return await _api.Post<DbAnime[], AnimeFilter>($"{ApiUrl}/anime/random", search) ?? Array.Empty<DbAnime>();
	}

	public async Task<PaginatedResult<DbAnime>> Search(AnimeFilter search)
	{
		return await _api.Post<PaginatedResult<DbAnime>, AnimeFilter>($"{ApiUrl}/anime", search)
			?? new PaginatedResult<DbAnime>(0, 0, Array.Empty<DbAnime>());
	}

	public Task<Filter[]?> Filters()
	{
		return _api.Get<Filter[]>($"{ApiUrl}/filters");
	}

	public Task<DbDiscordLog?> PostMessage(SocketMessage message)
	{
		var attachments = message.Attachments.Select(t => new DbDiscordAttachment
		{
			Id = t.Id.ToString(),
			Filename = t.Filename ?? string.Empty,
			Url = t.Url ?? string.Empty,
			Type = t.ContentType ?? string.Empty,
			Description = t.Description ?? string.Empty,
		}).ToArray();

		var stickers = message.Stickers.Select(t => new DbDiscordSticker
		{
			Id = t.Id.ToString(),
			Name = t.Name ?? string.Empty,
			Description = t.Description ?? string.Empty,
			Type = (int)t.Type,
			Format = (int)t.Format,
			Url = t.GetStickerUrl(),
		}).ToArray();

		var log = new DbDiscordLog
		{
			MessageId = message.Id.ToString(),
			AuthorId = message.Author.Id.ToString(),
			ChannelId = message.Channel?.Id.ToString(),
			GuildId = (message.Channel as SocketGuildChannel)?.Guild.Id.ToString(),
			ThreadId = message.Thread?.Id.ToString(),
			ReferenceId = message.Reference?.MessageId.ToString(),
			SendTimestamp = message.Timestamp.UtcDateTime,
			Attachments = attachments,
			MentionedChannels = message.MentionedChannels.Select(t => t.Id.ToString()).ToArray(),
			MentionedRoles = message.MentionedRoles.Select(t => t.Id.ToString()).ToArray(),
			MentionedUsers = message.MentionedUsers.Select(t => t.Id.ToString()).ToArray(),
			Stickers = stickers,
			Content = message.Content,
			MessageType = (int)message.Type,
			MessageSource = (int)message.Source,
		};

		return PostMessage(log);
	}

	public Task<DbDiscordLog?> PostMessage(DbDiscordLog log)
	{
        return _api.Post<DbDiscordLog, DbDiscordLog>($"{ApiUrl}/discord/message", log);
    }
}