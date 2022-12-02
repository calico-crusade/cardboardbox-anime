namespace CardboardBox.Anime.Database
{
	using CardboardBox.Database;
	using Generation;

	public interface IDiscordGuildDbService
	{
		Task<DbDiscordGuildSettings> Fetch(long id);
		Task<DbDiscordGuildSettings[]> All();
		Task<DbDiscordGuildSettings?> Get(string guildId);
		Task<long> Upsert(DbDiscordGuildSettings item);
	}

	public class DiscordGuildDbService : OrmMapExtended<DbDiscordGuildSettings>, IDiscordGuildDbService
	{
		public override string TableName => "discord_guild_settings";

		private List<string> _upsert = new();

		public DiscordGuildDbService(
			IDbQueryBuilderService query, 
			ISqlService sql) : base(query, sql) { }


		public Task<DbDiscordGuildSettings?> Get(string guildId)
		{
			return _sql.Fetch<DbDiscordGuildSettings?>(
				"SELECT * FROM discord_guild_settings WHERE guild_id = :guildId", 
				new { guildId });
		}

		public Task<long> Upsert(DbDiscordGuildSettings item)
		{
			return FakeUpsert(item, TableName, _upsert,
				v => v.With(t => t.GuildId),
				v => v.With(t => t.Id),
				v => v.With(t => t.Id).With(t => t.CreatedAt));
		}
	}
}
