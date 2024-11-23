namespace CardboardBox.Anime.Database;

using CardboardBox.Database;
using Generation;

public interface IDiscordLogDbService
{
    Task<long> Insert(DbDiscordLog item);
}

internal class DiscordLogDbService : OrmMapExtended<DbDiscordLog>, IDiscordLogDbService
{
    public DiscordLogDbService(
        IDbQueryBuilderService query, 
        ISqlService sql) : base(query, sql) { }

    public override string TableName => "discord_message_logs";
}
