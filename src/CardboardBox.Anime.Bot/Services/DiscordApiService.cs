namespace CardboardBox.Anime.Bot.Services
{
	public interface IDiscordApiService
	{
		Task<DbDiscordGuildSettings[]> Settings();
		Task<DbDiscordGuildSettings?> Settings(ulong guildId);
		void ClearCache();
	}

	public class DiscordApiService : IDiscordApiService
	{
		private CacheItem<DbDiscordGuildSettings[]> _cache;
		private readonly IApiService _api;
		private readonly IConfiguration _config;

		public string ApiUrl => _config["CBA:Url"];

		public DiscordApiService(IApiService api, IConfiguration config)
		{
			_api = api;
			_config = config;
			_cache = new CacheItem<DbDiscordGuildSettings[]>(RawSettings);
		}

		public void ClearCache() => _cache = new CacheItem<DbDiscordGuildSettings[]>(RawSettings);

		public async Task<DbDiscordGuildSettings[]> RawSettings()
		{
			return await _api.Get<DbDiscordGuildSettings[]>($"{ApiUrl}/discord/settings") ?? Array.Empty<DbDiscordGuildSettings>();
		}

		public async Task<DbDiscordGuildSettings[]> Settings()
		{
			return await _cache.Get() ?? Array.Empty<DbDiscordGuildSettings>();
		}

		public async Task<DbDiscordGuildSettings?> Settings(ulong guildId)
		{
			return (await Settings())
				.FirstOrDefault(t => t.GuildId == guildId.ToString());
		}
	}
}
