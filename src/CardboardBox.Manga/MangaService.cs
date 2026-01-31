using System.IO.Compression;

namespace CardboardBox.Manga;

using Anime.Core.Models;
using Anime.Database;
using CardboardBox.Extensions;
using MangaDex;
using Match;
using Providers;

public interface IMangaService
{
	MangaProvider[] Providers { get; }

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

	Task<bool> ResetChapterPages(string mangaId, int chapterId, string? platformId);

	string GenerateHashId(string title);

	Task<(MemoryStream stream, string name)?> CreateZip(string mangaId, int chapterId, string? platformId);

	Task<MangaData?> Volumed(string id, string? pid, ChapterSortColumn sort, bool asc, bool canRead);

    Task<bool> ToggleRead(string id, string pid, params long[] chapters);
}

public class MangaService : IMangaService
{
	private readonly ILogger _logger;
	private readonly IMangaSource[] _sources;
	private readonly IMatchApiService _match;
	private readonly IApiService _api;
	private readonly IDbService _db;

	private IMangaDbService _manga => _db.Manga;

	private readonly IMangaDexService _md;

	public MangaProvider[] Providers => Sources()
		.Select(t => new MangaProvider 
		{ 
			Name = t.Provider, 
			Url = t.HomeUrl 
		})
		.ToArray();

	public MangaService(
		IDbService db,
		IMatchApiService match,
		IApiService api,
		ILogger<MangaService> logger,
		IMangaDexService md,
		IMangakakalotTvSource mangakakalot,
		IMangakakalotComSource mangakakalot2,
		IMangakakalotComAltSource mangakakalot3,
		IMangaDexSource mangaDex,
		IMangaClashSource mangaClash,
		IDarkScansSource dark,
		INhentaiSource nhentai,
		IMangaKatanaSource katana,
		IBattwoSource battwo,
        IChapmanganatoSource chap,
		ILikeMangaSource lkm,
		IWeebDexSource wd,
		IComixSource comix)
	{
		_db = db;
		_match = match;
		_api = api;
		_md = md;
		_logger = logger;
		_sources =
        [
            mangaDex,
			mangakakalot,
			mangaClash,
			nhentai,
			mangakakalot2,
			mangakakalot3,
			katana,
			dark,
			chap,
			lkm,
			wd,
			comix,
		];
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
		var db = await _manga.Search(filter, null, true);
		return db ?? new();
	}

	public async Task<string[]> MangaPages(long chapterId, bool refetch)
	{
		var chapter = await _manga.GetChapter(chapterId);
		return await MangaPages(chapter, refetch);
	}

	public async Task<string[]> MangaPages(DbMangaChapter? chapter, bool refetch)
	{
		if (chapter == null) return Array.Empty<string>();
		if (chapter.Pages.Length > 0 && !refetch) return chapter.Pages;

		var manga = await _manga.Get(chapter.MangaId);
		if (manga == null) return Array.Empty<string>();

		return await MangaPages(chapter, manga, refetch);
	}

	public async Task<string[]> MangaPages(DbMangaChapter chapter, DbManga manga, bool refetch)
	{
		try
		{
			if (chapter.Pages.Length > 0 && !refetch) return chapter.Pages;

			var (src, id) = DetermineSource(manga.Url);
			if (src == null || id == null) return Array.Empty<string>();

			var pages = src is IMangaUrlSource url ?
				await url.ChapterPages(chapter.Url) :
				await src.ChapterPages(manga.SourceId, chapter.SourceId);
			if (pages == null) return Array.Empty<string>();

			await _manga.SetPages(chapter.Id, pages.Pages);
			chapter.Pages = pages.Pages;
			return chapter.Pages;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get pages for chapter: {chapterId} >> manga: {mangaId}", chapter.Id, manga.Id);
			return Array.Empty<string>();
		}
	}

	public Task<PaginatedResult<DbManga>> Manga(int page, int size)
	{
		return _manga.Paginate(page, size);
	}

	public async Task<MangaWithChapters?> Manga(string url, string? platformId, bool forceUpdate = false)
	{
		var (src, id) = DetermineSource(url);
		if (src == null || id == null) return null;

		var manga = await _manga.Get(id);
		if (manga == null || forceUpdate) return await LoadManga(src, id, platformId);

		return await Manga(manga.Id, platformId);
	}

	public Task<MangaWithChapters?> Manga(long id, string? platformId)
	{
		return _manga.GetManga(id, platformId);
	}

	public async Task<MangaWithChapters?> LoadManga(IMangaSource src, string id, string? platformId)
	{
		var m = await src.Manga(id);
		if (m == null) return null;

		var pid = !string.IsNullOrEmpty(platformId) ? (await _db.Profiles.Fetch(platformId))?.Id : null;
		var manga = await ConvertManga(m, pid);
		await ConvertChapters(m, manga.Id).ToArrayAsync();
		await _db.Manga.UpdateChapterComputed();
		await _db.Manga.UpdateComputed();
		return await Manga(manga.Id, platformId);
	}

	public async Task<DbManga> ConvertManga(Manga manga, long? pid)
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
			SourceCreated = manga.SourceCreated,
			Attributes = manga.Attributes
				.Select(t => new DbMangaAttribute(t.Name, t.Value))
				.ToArray(),
			Uploader = pid,
			OrdinalVolumeReset = manga.OrdinalVolumeReset,
		};
		m.Id = await _manga.Upsert(m);
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

			chap.Id = await _manga.Upsert(chap, false);
			yield return chap;
		}
	}

	public async Task<MangaWorked[]> Updated(int count, string? platformId)
	{
		var needs = await _manga.FirstUpdated(count);

		return await needs.Select(async t =>
		{
			try
			{
				var (src, id) = DetermineSource(t.Url);
				if (src == null || id == null) return new MangaWorked(t, false);

				var res = await LoadManga(src, id, platformId);
				if (res is not null && res.Manga is not null)
                    return new(res.Manga, true);

				await _db.Manga.FakeUpdate(t.Id);
				_logger.LogWarning("Failed to update manga: {mangaId}", t.Id);
				return new(t, false);
			}
			catch (Exception ex)
			{
				await _db.Manga.FakeUpdate(t.Id);
				_logger.LogError(ex, "Failed to update manga: {mangaId}", t.Id);
                return new(t, false);
            }
		}).WhenAll();
	}

	public async Task<long?> ResolveId(string id)
	{
		if (long.TryParse(id, out var mid))
			return mid;

		return (await _manga.GetByHashId(id))?.Id;
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
			await _manga.SetPages(chapter.Id, chapter.Pages);
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

	public async Task<bool> ResetChapterPages(string mangaId, int chapterId, string? platformId)
	{
		var manga = await _manga.GetManga(mangaId, platformId);
		if (manga == null) return false;

		var chapter = manga.Chapters.FirstOrDefault(t => t.Id == chapterId);
		if (chapter == null) return false;

		var (src, id) = DetermineSource(manga.Manga.Url);
		if (src == null || id == null) return false;

		var pages = src is IMangaUrlSource us ?
			await us.ChapterPages(chapter.Url) :
			await src.ChapterPages(manga.Manga.SourceId, chapter.SourceId);

		if (pages == null || pages.Pages == null || pages.Pages.Length == 0) return false;

		await _manga.SetPages(chapterId, pages.Pages);
		return true;
	}

	public async Task<(MemoryStream stream, string name)?> CreateZip(string mangaId, int chapterId, string? platformId)
	{
		var manga = await _manga.GetManga(mangaId, platformId);
		if (manga == null) return null;

		var chapter = manga.Chapters.FirstOrDefault(t => t.Id == chapterId);
		if (chapter == null) return null;

		var pages = await MangaPages(chapter, manga.Manga, false);
		if (pages.Length == 0) return null;

		var ms = new MemoryStream();
		using var zip = new ZipArchive(ms, ZipArchiveMode.Create, true);
		int requests = 0;
		for (var i = 0; i < pages.Length; i++)
		{
			requests++;
			var proxy = ProxyUrl(pages[i], referer: manga.Manga.Referer);
			var (stream, _, name, _) = await _api.GetData(proxy);
			var entry = zip.CreateEntry($"{i}-{name}");
			using var entryIo = entry.Open();
			await stream.CopyToAsync(entryIo);
			await stream.DisposeAsync();

			if (requests < 25) continue;

			await Task.Delay(5000);
			requests = 0;
		}
		zip.Dispose();

        ms.Position = 0;
		return (ms, $"{manga.Manga.HashId}-{chapter.Ordinal}.zip");
	}

	public static IEnumerable<DbMangaChapter> FixVolumes(IEnumerable<DbMangaChapter> chapters)
	{
		var grouped = chapters.GroupBy(t => t.Ordinal);
        foreach (var group in grouped)
        {
			var chaps = group.ToArray();
			var volume = chaps.Select(t => t.Volume)
				.Distinct()
				.Where(t => t != null)
				.OrderBy(t => t)
				.FirstOrDefault();

			foreach(var chap in chaps)
			{
				chap.Volume ??= volume;
				yield return chap;
            }
        }
    }

	public IEnumerable<DbMangaChapter> Ordered(IEnumerable<DbMangaChapter> chap, ChapterSortColumn sort, bool asc, DbMangaProgress? progress, bool reset, bool canRead)
	{
        var byOrdinalAsc = () => reset ? chap.OrderBy(t => t.Ordinal).OrderBy(t => t.Volume ?? 99999) : chap.OrderBy(t => t.Ordinal);
		var byOrdinalDesc = () => reset ? chap.OrderByDescending(t => t.Ordinal).OrderByDescending(t => t.Volume ?? 99999) : chap.OrderByDescending(t => t.Ordinal);

		if (!canRead) 
			chap = chap.Select(t =>
			{
				t.Pages = [];
				return t;
			});

		return sort switch
		{
			ChapterSortColumn.Date => asc ? chap.OrderBy(t => t.CreatedAt) : chap.OrderByDescending(t => t.CreatedAt),
			ChapterSortColumn.Language => asc ? chap.OrderBy(t => t.Language) : chap.OrderByDescending(t => t.Language),
			ChapterSortColumn.Title => asc ? chap.OrderBy(t => t.Title) : chap.OrderByDescending(t => t.Title),
			ChapterSortColumn.Read => OrderByRead(chap, asc, progress, reset, canRead),
			_ => asc ? byOrdinalAsc() : byOrdinalDesc(),
		};
	}

	public IEnumerable<DbMangaChapter> OrderByRead(IEnumerable<DbMangaChapter> chap, bool asc, DbMangaProgress? progress, bool reset, bool canRead)
	{
		if (progress == null) return Ordered(chap, ChapterSortColumn.Ordinal, asc, progress, reset, canRead);

		var progs = progress.Read.ToDictionary(t => t.ChapterId, t => t);

		return asc 
			? chap.OrderBy(t => progs.ContainsKey(t.Id)) 
			: chap.OrderByDescending(t => progs.ContainsKey(t.Id));
	}

	public IEnumerable<Volume> Volumize(IEnumerable<DbMangaChapter> chapters, DbMangaProgress? progress, MangaStats? stats)
	{
		chapters = FixVolumes(chapters);
        var iterator = chapters.GetEnumerator();

		//Setup tracking stuff
		DbMangaChapter? chapter = null;
		Volume? volume = null;

		var progs = (progress?.Read ?? Array.Empty<DbMangaChapterProgress>()).ToDictionary(t => t.ChapterId, t => t.PageIndex);

		static Volume postfix(Volume volume)
		{
			volume.Read = volume.Chapters.All(t => t.Read);
			volume.InProgress = !volume.Read && volume.Chapters.Any(t => t.Read);
			return volume;
		};

		while(true)
		{
			//Ensure its not the EoS
			if (chapter == null && !iterator.MoveNext()) break;
			//Get the current chapter
			chapter = iterator.Current;
			//Get all of the grouped versions
			var (versions, last, index) = iterator.MoveUntil(chapter, t => t.Volume, t => t.Ordinal);

			//Shouldn't happen unless something went very wrong.
			if (versions.Length == 0) break;

			var firstChap = versions.First();

			//New volume started, create the wrapping object
			volume ??= new Volume { Name = firstChap.Volume };

			var read = versions.Any(t => progs.ContainsKey(t.Id));
			//Check to see if the current chapter has been read
			int? idx = versions.IndexOfNull(t => t.Id == progress?.MangaChapterId);
			var chap = new VolumeChapter
			{
				Read = read,
				ReadIndex = idx,
				Progress = idx != null ? stats?.PageProgress : null,
				PageIndex = idx != null ? progress?.PageIndex : null,
				Versions = versions,
			};

			volume.Chapters.Add(chap);

			//New volume started, return the old one
			if (index == 0 && volume != null)
			{
				yield return postfix(volume);
				volume = null;
			}

			chapter = last;
		}

		if (volume != null) yield return postfix(volume);
	}

	public async Task<MangaWithChapters?> GetData(string id, string? pid)
	{
		//Ensure a valid ID was passed
		if (string.IsNullOrEmpty(id)) return null;

		//Check if a random manga was requested
		if (id.ToLower().Trim() == "random") return await _manga.Random(pid);

		//Determine if the ID was passed
		if (long.TryParse(id, out var mid))
			return await _manga.GetManga(mid, pid);

		//Or the hash
		return await _manga.GetManga(id, pid);
	}

	public async Task<MangaData?> Volumed(string id, string? pid, ChapterSortColumn sort, bool asc, bool canRead)
	{
		var manga = await GetData(id, pid);
		if (manga == null) return null;

		//Fetch progress, stats, and other authed stuff
		//Skip fetching if the user isn't logged in
		var ext = string.IsNullOrEmpty(pid) ? null : await _manga.GetMangaExtended(manga.Manga.Id, pid);

		//Create a clone of manga data with extra fields
		var output = manga.Clone<MangaWithChapters, MangaData>();
		if (output == null) return null;

		//Order the chapters by the given sorts
		var chapters = Ordered(manga.Chapters, sort, asc, ext?.Progress, output.Manga.OrdinalVolumeReset, canRead);
		//Sort the chapters into volume collections (impacted by sorts)
		output.Volumes = Volumize(chapters, ext?.Progress, ext?.Stats).ToArray();
		//Pass through progress stuff
		output.Chapter = ext?.Chapter ?? manga.Chapters.FirstOrDefault() ?? new();
		output.Progress = ext?.Progress;
		output.Stats = ext?.Stats;
		output.VolumeIndex = ext?.Chapter == null ? 0 : output.Volumes.IndexOfNull(t => t.InProgress) ?? 0;

		return output;
	}

	public async Task<bool> ToggleRead(string id, string pid, params long[] chapters)
	{
		async Task<bool> DoUpdate(DbMangaProgress progress)
		{
			if (progress.Id == -1)
                return await _manga.InsertProgress(progress) > 0;
            
			await _manga.UpdateProgress(progress);
			return true;
		}

		var profile = await _db.Profiles.Fetch(pid);
		if (profile == null) return false;

		var manga = await _manga.GetManga(id, pid);
		if (manga == null) return false;

		var progress = await _manga.GetProgress(pid, id) ?? new DbMangaProgress
		{
			Id = -1,
            ProfileId = profile.Id,
            MangaId = manga.Manga.Id
        };

		//Toggle full list of chapters
		if (chapters.Length == 0)
		{
			progress.Read = progress.Read.Length == 0 ? manga.Chapters.Select(t => new DbMangaChapterProgress
			{
                ChapterId = t.Id,
                PageIndex = t.Pages.Length - 1
            }).ToArray() : Array.Empty<DbMangaChapterProgress>();

			return await DoUpdate(progress);
		}

		//Toggle specific chapters
		var read = progress.Read.ToList();
		foreach(var chapter in chapters)
		{
			var chap = manga.Chapters.FirstOrDefault(t => t.Id == chapter);
			if (chap == null) continue;

			var index = chap.Pages.Length - 1 < 0 ? 0 : chap.Pages.Length - 1;

			var exists = read.FirstOrDefault(t => t.ChapterId == chapter);
			if (exists == null)
			{
				read.Add(new DbMangaChapterProgress(chap.Id, index));
				continue;
			}

			read.Remove(exists);
		}

		progress.Read = read.ToArray();
		return await DoUpdate(progress);
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

public enum ChapterSortColumn
{
	Ordinal = 0,
	Date = 1,
	Language = 2,
	Title = 3,
	Read = 4
}
