using Dapper;

namespace CardboardBox.Anime.Database;

using CardboardBox.Database;
using Generation;

public interface IChapterDbService
{
	Task<long> Upsert(DbChapter chapter);

	Task<(int total, DbChapter[] results)> Chapters(string bookId, int page = 1, int size = 10);

	Task<(int total, DbBook[] results)> Books(int page = 1, int size = 100);

	Task<DbBook?> BookById(string id);

	Task<DbBook?> BookByUrl(string url);

	Task<(DbBook? book, DbChapterLimited[] chapters)> ChapterList(string bookId);
}

public class ChapterDbService : OrmMapExtended<DbChapter>, IChapterDbService
{
	private string? _upsertQuery;
	public override string TableName => "light_novels";

	public ChapterDbService(IDbQueryBuilderService query, ISqlService sql) : base(query, sql) { }

	public Task<long> Upsert(DbChapter chapter)
	{
		_upsertQuery ??= _query.Upsert<DbChapter, long>(TableName,
			(v) => v.With(t => t.HashId).With(t => t.BookId),
			(v) => v.With(t => t.Id),
			(v) => v.With(t => t.Id).With(t => t.CreatedAt),
			(v) => v.Id);

		return _sql.ExecuteScalar<long>(_upsertQuery, chapter);
	}

	public async Task<(int total, DbChapter[] results)> Chapters(string bookId, int page = 1, int size = 10)
	{
		var offset = (page - 1) * size;
		var query = $@"SELECT
    *
FROM light_novels
WHERE
    book_id = :bookId
ORDER BY ordinal ASC
LIMIT {size}
OFFSET {offset};

SELECT COUNT(*) FROM light_novels WHERE book_id = :bookId;";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(query, new { bookId });

		var res = (await rdr.ReadAsync<DbChapter>()).ToArray();
		var total = await rdr.ReadSingleAsync<int>();
		return (total, res);
	}

	public async Task<(int total, DbBook[] results)> Books(int page = 1, int size = 100)
	{
		var offset = (page - 1) * size;
		var query = $@"
WITH books AS (
    SELECT
        DISTINCT
        book_id as id,
        book as title,
        MAX(ordinal) as last_ordinal
    FROM light_novels a
    GROUP BY a.book_id, a.book
)
SELECT
    id,
    title,
    ( SELECT COUNT(*) FROM light_novels l WHERE b.id = l.book_id ) as chapters,
    ( SELECT MIN(l.created_at) FROM light_novels l WHERE b.id = l.book_id ) as created_at,
    ( SELECT MAX(l.updated_at) FROM light_novels l WHERE b.id = l.book_id ) as updated_at,
	( SELECT l.url FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_url,
	( SELECT l.id FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_id,
	( SELECT l.ordinal FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_ordinal
FROM books b
ORDER BY title ASC
LIMIT {size}
OFFSET {offset};

SELECT COUNT(DISTINCT book) from light_novels;";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(query);

		var res = (await rdr.ReadAsync<DbBook>()).ToArray();
		var total = await rdr.ReadSingleAsync<int>();
		return (total, res);
	}

	public async Task<DbBook?> BookById(string id)
	{
		const string QUERY = @"WITH books AS (
    SELECT
        DISTINCT
        book_id as id,
        book as title,
        MAX(ordinal) as last_ordinal
    FROM light_novels a
    GROUP BY a.book_id, a.book
)
SELECT
    id,
    title,
    ( SELECT COUNT(*) FROM light_novels l WHERE b.id = l.book_id ) as chapters,
    ( SELECT MIN(l.created_at) FROM light_novels l WHERE b.id = l.book_id ) as created_at,
    ( SELECT MAX(l.updated_at) FROM light_novels l WHERE b.id = l.book_id ) as updated_at,
	( SELECT l.url FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_url,
	( SELECT l.id FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_id,
	( SELECT l.ordinal FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_ordinal
FROM books b
WHERE b.id = :id";

		return await _sql.Fetch<DbBook>(QUERY, new { id });
	}

	public async Task<DbBook?> BookByUrl(string url)
	{
		const string QUERY = @"WITH books AS (
    SELECT
        DISTINCT
        book_id as id,
        book as title,
        MAX(ordinal) as last_ordinal
    FROM light_novels a
	WHERE a.url = :url
    GROUP BY a.book_id, a.book
)
SELECT
    id,
    title,
    ( SELECT COUNT(*) FROM light_novels l WHERE b.id = l.book_id ) as chapters,
    ( SELECT MIN(l.created_at) FROM light_novels l WHERE b.id = l.book_id ) as created_at,
    ( SELECT MAX(l.updated_at) FROM light_novels l WHERE b.id = l.book_id ) as updated_at,
	( SELECT l.url FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_url,
	( SELECT l.id FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_id,
	( SELECT l.ordinal FROM light_novels l WHERE b.id = l.book_id AND l.ordinal = b.last_ordinal ) as last_chapter_ordinal
FROM books b";

		return await _sql.Fetch<DbBook>(QUERY, new { url });
	}

	public async Task<(DbBook? book, DbChapterLimited[] chapters)> ChapterList(string bookId)
	{
		const string QUERY = @"WITH books AS (
    SELECT
    DISTINCT
    book_id as id,
    book as title
    FROM light_novels
)
SELECT
    id,
    title,
    ( SELECT COUNT(*) FROM light_novels l WHERE b.id = l.book_id ) as chapters,
    ( SELECT MIN(l.created_at) FROM light_novels l WHERE b.id = l.book_id ) as created_at,
    ( SELECT MAX(l.updated_at) FROM light_novels l WHERE b.id = l.book_id ) as updated_at,
	( SELECT l.url FROM light_novels l WHERE b.id = l.book_id AND l.next_url = '' ) as last_chapter_url,
	( SELECT l.id FROM light_novels l WHERE b.id = l.book_id AND l.next_url = '' ) as last_chapter_id,
	( SELECT l.ordinal FROM light_novels l WHERE b.id = l.book_id AND l.next_url = '' ) as last_chapter_ordinal
FROM books b
WHERE b.id = :bookId;

SELECT
	id,
	created_at,
	updated_at,
	deleted_at,
	hash_id,
	chapter,
	ordinal
FROM light_novels
WHERE
	book_id = :bookId
ORDER BY ordinal";

		using var con = _sql.CreateConnection();
		using var rdr = await con.QueryMultipleAsync(QUERY, new { bookId });

		var res = await rdr.ReadFirstOrDefaultAsync<DbBook>();
		var chaps = (await rdr.ReadAsync<DbChapterLimited>()).ToArray();
		return (res, chaps);
	}
}
