namespace CardboardBox.Anime.Bot.Services;

using Database.Generation;

public interface IDbService
{
	Task<long> Upsert(LookupRequest request);

	Task<LookupRequest?> Fetch(string messageId);
}

public class DbService : OrmMapExtended<LookupRequest>, IDbService
{
	public override string TableName => "lookup_requests";

	private readonly List<string> _upsertLookupRequests = new();

	public DbService(
		IDbQueryBuilderService query,
		ISqlService sql) : base(query, sql) { }

	public Task<long> Upsert(LookupRequest request)
	{
		return FakeUpsert(request, TableName, _upsertLookupRequests,
			(v) => v.With(t => t.MessageId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt));
	}

	public Task<LookupRequest?> Fetch(string messageId)
	{
		var QUERY = $"SELECT * FROM {TableName} WHERE message_id = @messageId AND deleted_at IS NULL";
		return _sql.Fetch<LookupRequest?>(QUERY, new { messageId });
	}
}
