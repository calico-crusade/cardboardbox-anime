namespace CardboardBox.Anime.Database
{
	using Generation;

	public interface IAiRequestDbService
	{
		Task<long> Insert(DbAiRequest request);

		Task Update(DbAiRequest request);

		Task<PaginatedResult<DbAiRequest>> Paged(long? profileId = null, int page = 1, int size = 100);
	}

	public class AiRequestDbService : OrmMapExtended<DbAiRequest>, IAiRequestDbService
	{
		public override string TableName => "ai_requests";

		public AiRequestDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<PaginatedResult<DbAiRequest>> Paged(long? profileId = null, int page = 1, int size = 100)
		{
			const string query = @"SELECT 
	*
FROM ai_requests
WHERE
	:profileId IS NULL OR
	profile_id = :profileId
ORDER BY created_at DESC
LIMIT :size
OFFSET :offset;

SELECT 
	COUNT(*) 
FROM ai_requests 
WHERE
	:profileId IS NULL OR
	profile_id = :profileId";

			return Paginate(query, new { profileId }, page, size);
		}
	}
}
