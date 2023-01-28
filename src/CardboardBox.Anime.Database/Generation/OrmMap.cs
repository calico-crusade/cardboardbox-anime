using Dapper;

namespace CardboardBox.Anime.Database.Generation;

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
	private string? _paginateQuery;

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

	public virtual async Task<PaginatedResult<T>> Paginate(string query, object? pars = null, int page = 1, int size = 100)
	{
		var p = new DynamicParameters(pars);
		p.Add("offset", (page - 1) * size);
		p.Add("size", size);

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(query, p);

		var res = (await rdr.ReadAsync<T>()).ToArray();
		var total = await rdr.ReadSingleAsync<long>();

		var pages = (long)Math.Ceiling((double)total / size);
		return new PaginatedResult<T>(pages, total, res);
	}

	public virtual Task<PaginatedResult<T>> Paginate(int page = 1, int size = 100)
	{
		_paginateQuery ??= _query.Pagniate<T, long>(TableName, (c) => { }, t => t.Id);
		return Paginate(_paginateQuery, null, page, size);
	}

	public async Task<long> FakeUpsert<T2>(T2 item, string table, List<string> queryCache,
		Action<PropertyExpressionBuilder<T2>> conflicts,
		Action<PropertyExpressionBuilder<T2>>? inserts = null,
		Action<PropertyExpressionBuilder<T2>>? updates = null) where T2 : DbObject
	{
		//Note: This is purely to combat the issue of postgres SERIAL and BIGSERIAL 
		//		primary keys incrementing even if it was an update was preformed
		//		because the record already exists
		if (queryCache.Count != 3)
		{
			queryCache.Clear();
			queryCache.Add(_query.Update(table, updates));
			queryCache.Add(_query.InsertReturn(table, v => v.Id, inserts));
			queryCache.Add(_query.Select(table, conflicts));
		}

		string update = queryCache[0], insert = queryCache[1], select = queryCache[2];

		var exists = await _sql.Fetch<T>(select, item);
		if (exists == null)
			return await _sql.ExecuteScalar<long>(insert, item);

		item.Id = exists.Id;
		await _sql.Execute(update, item);
		return exists.Id;
	}
}
