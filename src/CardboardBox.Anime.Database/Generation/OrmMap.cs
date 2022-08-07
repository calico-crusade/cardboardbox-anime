namespace CardboardBox.Anime.Database.Generation
{
	public abstract class OrmMap<T>
	{
		private string? _allQuery;
		private string? _allNonDeletedQuery;

		public readonly IDbQueryBuilderService _query;
		public readonly ISqlService _sql;

		public abstract string TableName { get; }

		public OrmMap(
			IDbQueryBuilderService query,
			ISqlService sql)
		{
			_query = query;
			_sql = sql;
		}

		public virtual Task<T[]> All()
		{
			_allNonDeletedQuery ??= _query.SelectAllNonDeleted(TableName);
			return _sql.Get<T>(_allNonDeletedQuery);
		}

		public virtual Task<T[]> AllWithDeleted()
		{
			_allQuery ??= _query.SelectAll(TableName);
			return _sql.Get<T>(_allQuery);
		}
	}

	public abstract class OrmMapExtended<T> : OrmMap<T>
		where T : DbObject
	{
		private string? _fetchQuery;
		private string? _insertReturnQuery;
		private string? _insertQuery;
		private string? _updateQuery;

		public OrmMapExtended(
			IDbQueryBuilderService query,
			ISqlService sql) : base(query, sql) { }

		public virtual Task<T> Fetch(long id)
		{
			_fetchQuery ??= _query.SelectId<T>(TableName);
			return _sql.Fetch<T>(_fetchQuery, new { id });
		}

		public virtual Task<long> Insert(T obj)
		{
			_insertReturnQuery ??= _query.InsertReturn<T, long>(TableName, t => t.Id);
			return _sql.ExecuteScalar<long>(_insertReturnQuery, obj);
		}

		public virtual Task InsertNoReturn(T obj)
		{
			_insertQuery ??= _query.Insert<T>(TableName);
			return _sql.Execute(_insertQuery, obj);
		}

		public virtual Task Update(T obj)
		{
			_updateQuery ??= _query.Update<T>(TableName);
			return _sql.Execute(_updateQuery, obj);
		}
	}
}
