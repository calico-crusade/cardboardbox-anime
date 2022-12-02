using CardboardBox.Http;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.Configuration;

namespace CardboardBox.Anime.DiscordIntermediary
{
	public interface IDiscordClient
	{
		Task<DiscordRestClient> GetClient();

		Task<FakeGuild[]> GetGuilds();

		Task<FakeGuild> GetGuild(ulong id);

		Task<FakeUser> GetUser(ulong id);

		Task<FakeGuildUser> GetUser(ulong id, ulong guildId);
	}

	public class DiscordClient : IDiscordClient
	{
		private static readonly DiscordRestClient _client = new();

		private readonly IConfiguration _config;
		private readonly CacheItem<RestGuild[]> _guilds;

		private string Token => _config["Discord:Token"];

		public DiscordClient(
			IConfiguration config)
		{
			_config = config;
			_guilds = new CacheItem<RestGuild[]>(RawGuilds);
		}

		public async Task<DiscordRestClient> GetClient()
		{
			if (_client.LoginState != LoginState.LoggedIn)
				await _client.LoginAsync(TokenType.Bot, Token);

			return _client;
		}

		public async Task<FakeGuild[]> GetGuilds()
		{
			var guilds = await _guilds.Get() ?? Array.Empty<RestGuild>();
			return guilds.Select(t => (FakeGuild)t).ToArray();
		}

		public async Task<RestGuild[]> RawGuilds()
		{
			var client = await GetClient();
			return (await client.GetGuildsAsync()).ToArray();
		}

		public async Task<FakeGuild> GetGuild(ulong id)
		{
			var client = await GetClient();
			var guild = await client.GetGuildAsync(id);
			return (FakeGuild)guild;
		}

		public async Task<FakeUser> GetUser(ulong id)
		{
			var client = await GetClient();
			var user = await client.GetUserAsync(id);
			return (FakeUser)user;
		}

		public async Task<FakeGuildUser> GetUser(ulong id, ulong guildId)
		{
			var client = await GetClient();
			var user = await client.GetGuildUserAsync(guildId, id);
			return (FakeGuildUser)user;
		}
	}
}
