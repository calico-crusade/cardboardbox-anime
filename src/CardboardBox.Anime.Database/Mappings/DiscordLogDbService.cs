namespace CardboardBox.Anime.Database;

using CardboardBox.Database;
using Generation;

public interface IDiscordLogDbService
{
    Task<long> Insert(DbDiscordLog item);

    Task<DeleteCount> Delete(string messageId);
}

internal class DiscordLogDbService : OrmMapExtended<DbDiscordLog>, IDiscordLogDbService
{
    public DiscordLogDbService(
        IDbQueryBuilderService query, 
        ISqlService sql) : base(query, sql) { }

    public override string TableName => "discord_message_logs";

    public Task<DeleteCount> Delete(string messageId)
    {
        const string QUERY = @"UPDATE discord_message_logs 
SET deleted_at = CURRENT_TIMESTAMP 
WHERE message_id = :messageId";
        return _sql.Execute(QUERY, new { messageId }).ContinueWith(t => new DeleteCount(t.Result));
    }
}

public record class DeleteCount([property: JsonPropertyName("count")] int Count);
