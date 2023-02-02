namespace CardboardBox.Manga;

using Anime.Core.Models;
using Anime.Database;
using MangaDex;
using Match;
using Providers;

public interface IMangaService
{
	IMangaSource[] Sources();
	(IMangaSource? source, string? id) DetermineSource(string url);

	Task<PaginatedResult<DbManga>> Manga(int page, int size);
	Task<MangaWithChapters?> Manga(string url, string? platformId, bool forceUpdate = false);
	Task<MangaWithChapters?> Manga(long id, string? platformId);
	Task<string[]> MangaPages(long chapterId, bool refetch);
	Task<string[]> MangaPages(DbMangaChapter? chapter, bool refetch);
	Task<string[]> MangaPages(DbMangaChapter chapter, DbManga manga, bool refetch);
	Task<MangaWorked[]> Updated(int count, string? platformId);
	Task<long?> ResolveId(string id);
	Task<(bool worked, bool indexed)> IndexChapter(DbManga manga, DbMangaChapter chapter);
	Task<PaginatedResult<MangaProgress>> All();

	string GenerateHashId(string title);
}

public class MangaService : IMangaService
{
	private readonly IMangaSource[] _sources;
	private readonly IMangaDbService _db;
	private readonly IMatchApiService _match;

	private readonly IMangaDexService _md;

	public MangaService(
		IMangaDbService db,
		IMatchApiService match,
		IMangaDexService md,
		IMangakakalotTvSource mangakakalot, 
		IMangakakalotComSource mangakakalot2,
		IMangakakalotComAltSource mangakakalot3,
		IMangaDexSource mangaDex,
		IMangaClashSource mangaClash,
		INhentaiSource nhentai,
		IBattwoSource battwo)
	{
		_db = db;
		_match = match;
		_md = md;
		_sources = new IMangaSource[]
		{
			mangaDex,
			mangakakalot,
			mangaClash,
			nhentai,
			mangakakalot2,
			mangakakalot3
		};
	}

	public IMangaSource[] Sources() => _sources;

	public (IMangaSource? source, string? id) DetermineSource(string url)
	{
		foreach (var source in Sources())
		{
			var (worked, id) = source.MatchesProvider(url);
			if (worked) return (source, id);
		}

		return (null, null);
	}

	public async Task<PaginatedResult<MangaProgress>> All()
	{
		var filter = new MangaFilter
		{
			Size = 9999999,
			Nsfw = NsfwCheck.DontCare
		};
		var db = await _db.Search(filter, null);
		return db ?? new();
	}

	public async Task<string[]> MangaPages(long chapterId, bool refetch)
	{
		var chapter = await _db.GetChapter(chapterId);
		return await MangaPages(chapter, refetch);
	}

	public async Task<string[]> MangaPages(DbMangaChapter? chapter, bool refetch)
	{
		if (chapter == null) return Array.Empty<string>();
		if (chapter.Pages.Length > 0 && !refetch) return chapter.Pages;

		var manga = await _db.Get(chapter.MangaId);
		if (manga == null) return Array.Empty<string>();

		return await MangaPages(chapter, manga, refetch);
	}

	public async Task<string[]> MangaPages(DbMangaChapter chapter, DbManga manga, bool refetch)
	{
		if (chapter.Pages.Length > 0 && !refetch) return chapter.Pages;

		var (src, id) = DetermineSource(manga.Url);
		if (src == null || id == null) return Array.Empty<string>();

		var pages = src is IMangaUrlSource url ?
			await url.ChapterPages(chapter.Url) :
			await src.ChapterPages(manga.SourceId, chapter.SourceId);
		if (pages == null) return Array.Empty<string>();

		await _db.SetPages(chapter.Id, pages.Pages);
		chapter.Pages = pages.Pages;
		return chapter.Pages;
	}

	public Task<PaginatedResult<DbManga>> Manga(int page, int size)
	{
		return _db.Paginate(page, size);
	}

	public async Task<MangaWithChapters?> Manga(string url, string? platformId, bool forceUpdate = false)
	{
		var (src, id) = DetermineSource(url);
		if (src == null || id == null) return null;

		var manga = await _db.Get(id);
		if (manga == null || forceUpdate) return await LoadManga(src, id, platformId);

		return await Manga(manga.Id, platformId);
	}

	public Task<MangaWithChapters?> Manga(long id, string? platformId)
	{
		return _db.GetManga(id, platformId);
	}

	public async Task<MangaWithChapters?> LoadManga(IMangaSource src, string id, string? platformId)
	{
		var m = await src.Manga(id);
		if (m == null) return null;

		var manga = await ConvertManga(m);
		await ConvertChapters(m, manga.Id).ToArrayAsync();
		return await Manga(manga.Id, platformId);
	}

	public async Task<DbManga> ConvertManga(Manga manga)
	{
		var m = new DbManga
		{
			Title = manga.Title,
			HashId = GenerateHashId(manga.Provider + " " + manga.Title).ToLower(),
			SourceId = manga.Id,
			Provider = manga.Provider,
			Url = manga.HomePage,
			Cover = manga.Cover,
			Tags = manga.Tags,
			Description = manga.Description,
			AltTitles = manga.AltTitles,
			Nsfw = manga.Nsfw,
			Referer = manga.Referer,
			Attributes = manga.Attributes
				.Select(t => new DbMangaAttribute(t.Name, t.Value))
				.ToArray()
		};
		m.Id = await _db.Upsert(m);
		return m;
	}

	public string GenerateHashId(string title)
	{
		return Regex.Replace(title, "[^a-zA-Z0-9 ]", string.Empty).Replace(" ", "-");
	}

	public async IAsyncEnumerable<DbMangaChapter> ConvertChapters(Manga manga, long id, string language = "en")
	{
		foreach (var chapter in manga.Chapters)
		{
			var chap = new DbMangaChapter
			{
				MangaId = id,
				Title = chapter.Title,
				Url = chapter.Url,
				SourceId = chapter.Id,
				Ordinal = chapter.Number,
				Volume = chapter.Volume,
				Language = language,
				ExternalUrl = chapter.ExternalUrl,
				Attributes = chapter.Attributes
					.Select(t => new DbMangaAttribute(t.Name, t.Value))
					.ToArray()
			};

			chap.Id = await _db.Upsert(chap);
			yield return chap;
		}
	}

	public async Task<MangaWorked[]> Updated(int count, string? platformId)
	{
		var needs = await _db.FirstUpdated(count);

		return await needs.Select(async t =>
		{
			var (src, id) = DetermineSource(t.Url);
			if (src == null || id == null) return new MangaWorked(t, false);

			var res = await LoadManga(src, id, platformId);
			return new(res?.Manga ?? t, res != null);
		}).WhenAll();
	}

	public async Task<long?> ResolveId(string id)
	{
		if (long.TryParse(id, out var mid))
			return mid;

		return (await _db.GetByHashId(id))?.Id;
	}

	public string IndexId(DbManga manga, DbMangaChapter chapter, int i)
	{
		return $"manga-page:{manga.Id}-{chapter.Id}-{i}";
	}

	public async Task<(bool worked, bool indexed)> IndexChapter(DbManga manga, DbMangaChapter chapter)
	{
		bool indexed = false;
		if (chapter.Pages.Length == 0)
		{
			chapter.Pages = (await _md.Pages(chapter.SourceId))?.Images ?? Array.Empty<string>();
			indexed = true;

			if (chapter.Pages.Length == 0) return (false, indexed);
			await _db.SetPages(chapter.Id, chapter.Pages);
		}

		return (true, indexed);

		//var urlResolver = (string url) => ProxyUrl(url, referer: manga.Referer);

		//var results = await chapter.Pages.Select(async (t, i) =>
		//{
		//	var url = urlResolver(t);
		//	var id = IndexId(manga, chapter, i);
		//	var res = await _match.Add(url, id);

		//	return res != null && res.Success;
		//}).WhenAll();

		//return (results.All(t => t), indexed);
	}

	public string ProxyUrl(string url, string group = "manga-page", string? referer = null)
	{
		var path = WebUtility.UrlEncode(url);
		var uri = $"https://cba-proxy.index-0.com/proxy?path={path}&group={group}";
		if (!string.IsNullOrEmpty(referer))
			uri += $"&referer={WebUtility.UrlEncode(referer)}";

		return uri;
	}
}

public class MangaWorked
{
	[JsonPropertyName("manga")]
	public DbManga Manga { get; set; } = new();

	[JsonPropertyName("worked")]
	public bool Worked { get; set; } = false;

	public MangaWorked() { }

	public MangaWorked(DbManga manga, bool worked)
	{
		Manga = manga;
		Worked = worked;
	}
}
