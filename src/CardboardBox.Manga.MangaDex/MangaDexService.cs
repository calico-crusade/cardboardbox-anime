namespace CardboardBox.Manga.MangaDex
{
	using Models;

	public interface IMangaDexService
	{
		Task<MangaDexCollection<MangaDexManga>?> AllManga(params string[] ids);

		Task<MangaDexRoot<MangaDexManga>?> Manga(string id, string[]? includes = null);

		Task<MangaDexCollection<MangaDexManga>?> Search(MangaFilter filter);

		Task<MangaDexCollection<MangaDexManga>?> Search(string title);

		Task<MangaDexCollection<MangaDexChapter>?> Chapters(ChaptersFilter? filter = null);

		Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, ChaptersFilter? filter = null);

		Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, int limit = 500, int offset = 0);

		Task<MangaDexRoot<MangaDexChapter>?> Chapter(string id, string[]? includes = null);

		Task<MangaDexCollection<MangaDexChapter>?> ChaptersLatest(ChaptersFilter? filter = null);

		Task<MangaDexPages?> Pages(string id);
	}

	public class MangaDexService : IMangaDexService
	{
		private readonly IApiService _api;

		public MangaDexService(IApiService api)
		{
			_api = api;
		}

		public Task<MangaDexCollection<MangaDexManga>?> Search(string title)
		{
			var filter = new MangaFilter { Title = title };
			return Search(filter);
		}

		public Task<MangaDexCollection<MangaDexManga>?> Search(MangaFilter filter)
		{
			var url = $"https://api.mangadex.org/manga?{filter.BuildQuery()}";
			return _api.Get<MangaDexCollection<MangaDexManga>>(url);
		}

		public Task<MangaDexCollection<MangaDexManga>?> AllManga(params string[] ids)
		{
			var filter = new MangaFilter { Ids = ids };
			return Search(filter);
		}

		public Task<MangaDexRoot<MangaDexManga>?> Manga(string id, string[]? includes = null)
		{
			includes ??= new[]
			{
				"cover_art", "author", "artist",
				"scanlation_group", "tag", "chapter"
			};
			var pars = string.Join("&", includes.Select(t => $"includes[]={t}"));
			var url = $"https://api.mangadex.org/manga/{id}?{pars}";
			return _api.Get<MangaDexRoot<MangaDexManga>>(url);
		}

		public Task<MangaDexCollection<MangaDexChapter>?> Chapters(ChaptersFilter? filter = null)
		{
			filter ??= new ChaptersFilter();
			var url = $"https://api.mangadex.org/chapter?{filter.BuildQuery()}";
			return _api.Get<MangaDexCollection<MangaDexChapter>>(url);
		}

		public Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, ChaptersFilter? filter = null)
		{
			var url = $"https://api.mangadex.org/manga/{id}/feed?{(filter ?? new()).BuildQuery()}";
			return _api.Get<MangaDexCollection<MangaDexChapter>>(url);
		}

		public Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, int limit = 500, int offset = 0)
		{
			var filter = new ChaptersFilter
			{
				Includes = new[] { "scanlation_group", "user" },
				Order = new()
				{
					[ChaptersFilter.OrderKey.volume] = ChaptersFilter.OrderValue.asc,
					[ChaptersFilter.OrderKey.chapter] = ChaptersFilter.OrderValue.asc,
				},
				ContentRating = ChaptersFilter.ContentRatingsAll,
				Limit = limit,
				Offset = offset
			};

			return Chapters(id, filter);
		}

		public Task<MangaDexCollection<MangaDexChapter>?> ChaptersLatest(ChaptersFilter? filter = null)
		{
			filter ??= new ChaptersFilter();
			filter.ContentRating = ChaptersFilter.ContentRatingsAll;
			filter.Limit = 100;
			filter.Order = new() { [ChaptersFilter.OrderKey.updatedAt] = ChaptersFilter.OrderValue.desc };
			filter.Includes = new[] { "manga" };
			filter.TranslatedLanguage = new[] { "en" };
			return Chapters(filter);
		}

		public Task<MangaDexRoot<MangaDexChapter>?> Chapter(string id, string[]? includes = null)
		{
			//https://api.mangadex.org/chapter/b5c27796-f334-43b5-834f-6617f7458d0b
			includes ??= new[]
			{
				"scanlation_group", "manga", "user"
			};
			var pars = string.Join("&", includes.Select(t => $"includes[]={t}"));
			var url = $"https://api.mangadex.org/chapter/{id}?{pars}";
			return _api.Get<MangaDexRoot<MangaDexChapter>>(url);
		}

		public Task<MangaDexPages?> Pages(string id)
		{
			var url = $"https://api.mangadex.org/at-home/server/{id}?forcePort443=false";
			return _api.Get<MangaDexPages>(url);
		}
	}
}
