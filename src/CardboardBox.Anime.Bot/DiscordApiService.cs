namespace CardboardBox.Anime.Bot
{
	public interface IDiscordApiService
	{
		Task<DbDiscordGuildSettings?> Settings(ulong guildId);
		void ClearCache();
	}

	public class DiscordApiService : IDiscordApiService
	{
		private Dictionary<ulong, CacheItem<DbDiscordGuildSettings?>> _cache = new();
		private readonly IApiService _api;
		private readonly IConfiguration _config;

		public string ApiUrl => _config["CBA:Url"];

		public DiscordApiService(IApiService api, IConfiguration config)
		{
			_api = api;
			_config = config;
		}

		public void ClearCache() => _cache.Clear();

		public Task<DbDiscordGuildSettings?> Settings(ulong guildId)
		{
			if (!_cache.ContainsKey(guildId))
				_cache.Add(guildId, new CacheItem<DbDiscordGuildSettings?>(() => RawSettings(guildId)));

			return _cache[guildId].Get();
		}

		public Task<DbDiscordGuildSettings?> RawSettings(ulong guildId)
		{
			return _api.Get<DbDiscordGuildSettings>($"{ApiUrl}/discord/settings/{guildId}");
		}
	}
}
