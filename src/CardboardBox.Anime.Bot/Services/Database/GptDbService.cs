namespace CardboardBox.Anime.Bot.Services;

using Database.Generation;

public interface IGptDbService
{
	Task<GptAuthorized[]> Get(string userId, string groupId);
	Task<GptAuthorized?> FetchUser(string userId);
	Task<GptAuthorized?> FetchGroup(string groupId);

	Task Toggle(string userId, string groupId, string type = GptAuthorized.WHITE_LIST);
	Task ToggleUser(string userId, string type = GptAuthorized.WHITE_LIST);
	Task ToggleGroup(string groupId, string type = GptAuthorized.WHITE_LIST);

	Task<bool> ValidateUser(ulong userId, ulong? guildId, ulong[] authorized);
}

public class GptDbService : OrmMapExtended<GptAuthorized>, IGptDbService
{
	public override string TableName => "gpt_authorized";

	public GptDbService(
		IDbQueryBuilderService query,
		ISqlService sql) : base(query, sql) { }

	public Task<GptAuthorized?> FetchUser(string userId)
	{
		const string QUERY = "SELECT * FROM gpt_authorized WHERE user_id = :userId AND server_id = '0'";
		return _sql.Fetch<GptAuthorized?>(QUERY, new { userId });
	}

	public Task<GptAuthorized?> FetchGroup(string groupId)
	{
		const string QUERY = "SELECT * FROM gpt_authorized WHERE server_id = :groupId AND user_id  = '0'";
		return _sql.Fetch<GptAuthorized?>(QUERY, new { groupId });
	}

	public Task<GptAuthorized[]> Get(string userId, string groupId)
	{
		const string QUERY = "SELECT * FROM gpt_authorized WHERE " +
			"(server_id = :groupId AND user_id = '0') OR " +
			"(user_id = :userId AND server_id = '0') OR " +
			"(user_id = :userId AND server_id = :groupId)";

		return _sql.Get<GptAuthorized>(QUERY, new { userId, groupId });
	}

	public Task Toggle(string userId, string groupId, string type = GptAuthorized.WHITE_LIST)
	{
		const string QUERY = "REPLACE INTO gpt_authorized(user_id, server_id, type, updated_at) VALUES (:userId, :groupId, :type, datetime('now'))";
		return _sql.Execute(QUERY, new { userId, groupId, type });
	}

	public Task ToggleUser(string userId, string type = GptAuthorized.WHITE_LIST)
	{
		const string QUERY = "REPLACE INTO gpt_authorized(user_id, type, updated_at) VALUES (:userId, :type, datetime('now'))";
		return _sql.Execute(QUERY, new { userId, type });
	}

	public Task ToggleGroup(string groupId, string type = GptAuthorized.WHITE_LIST)
	{
		const string QUERY = "REPLACE INTO gpt_authorized(server_id, type, updated_at) VALUES (:groupId, :type, datetime('now'))";
		return _sql.Execute(QUERY, new { groupId, type });
	}

	public async Task<bool> ValidateUser(ulong userId, ulong? guildId, ulong[] authorized)
	{
		if (authorized.Contains(userId)) return true;

		if (guildId == null)
		{
			var mark = await FetchUser(userId.ToString());
			if (mark == null || mark.Type != GptAuthorized.WHITE_LIST) return false;

			return true;
		}

		var marks = await Get(userId.ToString(), guildId.Value.ToString());
		if (marks.Length == 0 || marks.Any(t => t.Type != GptAuthorized.WHITE_LIST)) return false;

		return true;
	}
}
