namespace CardboardBox.Anime.Database;

using Generation;

public interface IProfileDbService
{
	Task<long> Upsert(DbProfile profile);
	Task<DbProfile> Fetch(string platformId);
	Task UpdateSettings(string platformId, string settings);
}

public class ProfileDbService : OrmMapExtended<DbProfile>, IProfileDbService
{
	private static string? _upsertQuery;
	private static string? _getQuery;

	public override string TableName => "profiles";

	public ProfileDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public Task<long> Upsert(DbProfile profile)
	{
		_upsertQuery ??= _query.Upsert<DbProfile, long>(TableName,
			(v) => v.With(t => t.PlatformId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt).With(t => t.Admin).With(t => t.SettingsBlob).With(t => t.UiApproval),
			(v) => v.Id);

		return _sql.ExecuteScalar<long>(_upsertQuery, profile);
	}

	public Task<DbProfile> Fetch(string platformId)
	{
		_getQuery ??= _query.Select<DbProfile>(TableName, t => t.With(t => t.PlatformId));

		return _sql.Fetch<DbProfile>(_getQuery, new { platformId });
	}

	public Task UpdateSettings(string platformId, string settings)
	{
		const string QUERY = "UPDATE profiles SET settings_blob = :settings WHERE platform_id = :platformId;";
		return _sql.Execute(QUERY, new { platformId, settings });
	}
}
