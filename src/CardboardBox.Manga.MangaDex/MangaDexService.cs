using MangaDexSharp;

namespace CardboardBox.Manga.MangaDex;

using MManga = MangaDexSharp.Manga;

public interface IMangaDexService
{
	Task<MangaList> AllManga(params string[] ids);

	Task<MangaDexRoot<MManga>> Manga(string id);

	Task<MangaList> Search(MangaFilter filter);

	Task<MangaList> Search(string title);

	Task<ChapterList> Chapters(ChaptersFilter? filter = null);

	Task<ChapterList> Chapters(string id, MangaFeedFilter? filter = null);

	Task<ChapterList> Chapters(string id, int limit = 500, int offset = 0);

	Task<MangaDexRoot<Chapter>> Chapter(string id, string[]? includes = null);

	Task<ChapterList> ChaptersLatest(ChaptersFilter? filter = null);

	Task<Pages> Pages(string id);
}

public class MangaDexService : IMangaDexService
{
	private readonly IMangaDex _md;
	private readonly ILogger _logger;

	public MangaDexService(
		IMangaDex md,
		ILogger<MangaDexService> logger)
	{
		_md = md;
		_logger = logger;
	}

	public Task<MangaList> Search(string title) => Search(new MangaFilter() { Title = title });

	public Task<MangaList> Search(MangaFilter filter) => _md.Manga.List(filter);

	public Task<MangaList> AllManga(params string[] ids) => Search(new MangaFilter { Ids = ids });

	public Task<MangaDexRoot<MManga>> Manga(string id)
	{
		return _md.Manga.Get(id, new[] { MangaIncludes.cover_art, MangaIncludes.author, MangaIncludes.artist, MangaIncludes.scanlation_group, MangaIncludes.tag, MangaIncludes.chapter});
	}

	public Task<ChapterList> Chapters(ChaptersFilter? filter = null) => _md.Chapter.List(filter);

	public Task<ChapterList> Chapters(string id, MangaFeedFilter? filter = null) => _md.Manga.Feed(id, filter);

	public Task<ChapterList> Chapters(string id, int limit = 500, int offset = 0)
	{
		var filter = new MangaFeedFilter
		{
			Order = new()
			{
				[MangaFeedFilter.OrderKey.volume] = OrderValue.asc,
				[MangaFeedFilter.OrderKey.chapter] = OrderValue.asc,
			},
			Limit = limit,
			Offset = offset
		};

		return Chapters(id, filter);
	}

	public Task<ChapterList> ChaptersLatest(ChaptersFilter? filter = null)
	{
		filter ??= new ChaptersFilter();
		filter.Limit = 100;
		filter.Order = new() { [ChaptersFilter.OrderKey.updatedAt] = OrderValue.desc };
		filter.Includes = new[] { MangaIncludes.manga };
		filter.TranslatedLanguage = new[] { "en" };
		filter.IncludeExternalUrl = false;
		return Chapters(filter);
	}

	public Task<MangaDexRoot<Chapter>> Chapter(string id, string[]? includes = null)
	{
		return _md.Chapter.Get(id, includes);
	}

	public async Task<Pages> Pages(string id)
	{
		try
		{
			return await _md.Pages.Pages(id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while getting pages for {Id}", id);
			return new Pages() { };
		}
	}
}
