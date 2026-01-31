namespace CardboardBox.Anime.Database;

using Dapper;
using Generation;

public interface IChatDbService
{
	Task<DbChat[]> Chats(string? platformId);

	Task<DbChat> Fetch(long id);

	Task<long> Insert(DbChat chat);

	Task<long> Insert(DbChatMessage message);

	Task Update(DbChat chat);

	Task Update(DbChatMessage message);

	Task<DbChatData?> Chat(long id);
}

public class ChatDbService : OrmMapExtended<DbChat>, IChatDbService
{
	private static string? _queryInsertMessage;
	private static string? _queryUpdateMessage;

	private const string CHAT_MESSAGE = "chat_message";
	public override string TableName => "chat";

	public ChatDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public Task<DbChat[]> Chats(string? platformId)
	{
		const string QUERY = @"SELECT c.* FROM chat c
JOIN profiles p ON p.id = c.profile_id
WHERE
	p.platform_id = :platformId AND
	p.deleted_at IS NULL AND
	c.deleted_at IS NULL";
		return _sql.Get<DbChat>(QUERY, new { platformId });
	}

	public Task<long> Insert(DbChatMessage message)
	{
		_queryInsertMessage ??= _query.Insert<DbChatMessage>(CHAT_MESSAGE);
		return _sql.ExecuteScalar<long>(_queryInsertMessage, message);
	}

	public Task Update(DbChatMessage message)
	{
		_queryUpdateMessage ??= _query.Update<DbChatMessage>(CHAT_MESSAGE);
		return _sql.Execute(_queryUpdateMessage, message);
	}

	public async Task<DbChatData?> Chat(long id)
	{
		const string QUERY = "SELECT * FROM chat WHERE id = :id; SELECT * FROM chat_message WHERE chat_id = :id;";

		using var con = await _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { id });

		var chat = await rdr.ReadFirstOrDefaultAsync<DbChat>();
		if (chat == null) return null;

		var messages = await rdr.ReadAsync<DbChatMessage>();
		return new DbChatData
		{
			Chat = chat,
			Messages = messages.ToArray()
		};
	}
}
