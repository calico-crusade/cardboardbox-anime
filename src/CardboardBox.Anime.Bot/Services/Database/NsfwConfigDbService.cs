namespace CardboardBox.Anime.Bot.Services;

using Database.Generation;

public interface INsfwConfigDbService
{
	Task<long> Upsert(NsfwConfigState request);

	Task<NsfwConfigState?> Fetch(string guildId);

	Task<NsfwConfigState?> Fetch(ulong guildId);
}

public class NsfwConfigDbService : OrmMapExtended<NsfwConfig>, INsfwConfigDbService
{
	public override string TableName => "nsfw_config";
	private static string? _queryFetch;
	private static readonly List<string> _upsertLookupRequests = new();

	public NsfwConfigDbService(
		IDbQueryBuilderService query, 
		ISqlService sql) : base(query, sql) { }

	public Task<long> Upsert(NsfwConfigState request)
	{
		return FakeUpsert((NsfwConfig)request, TableName, _upsertLookupRequests,
			(v) => v.With(t => t.GuildId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt));
	}

	public async Task<NsfwConfigState?> Fetch(string guildId)
	{
		_queryFetch ??= _query.Select<NsfwConfig>(TableName, t => t.With(t => t.GuildId));
		var res = await _sql.Fetch<NsfwConfig?>(_queryFetch, new { GuildId = guildId });

		return res == null ? null : (NsfwConfigState)res;
	}

	public Task<NsfwConfigState?> Fetch(ulong guildId) => Fetch(guildId.ToString());
}
