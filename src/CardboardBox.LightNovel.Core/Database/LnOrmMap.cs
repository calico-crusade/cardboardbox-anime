using System.Linq.Expressions;

namespace CardboardBox.LightNovel.Core.Database
{
	using Anime.Database.Generation;

	public interface ILnOrmMap<T> where T : HashBase
	{
		Task<long> Upsert(T item);

		Task<T> Fetch(long id);

		Task<long> Insert(T obj);

		Task Update(T obj);

		Task<T[]> All();
	}

	public abstract class LnOrmMap<T> : OrmMapExtended<T>, ILnOrmMap<T> where T : HashBase
	{
		private string? _upsertQuery;

		public abstract Expression<Func<T, long>> FkId { get; }

		public LnOrmMap(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

		public Task<long> Upsert(T item)
		{
			_upsertQuery ??= _query.Upsert<T, long>(TableName,
				(v) => v.With(t => t.HashId).With(FkId),
				(v) => v.With(t => t.Id),
				(v) => v.With(t => t.Id).With(t => t.CreatedAt),
				(v) => v.Id);

			return _sql.ExecuteScalar<long>(_upsertQuery, item);
		}
	}
}
