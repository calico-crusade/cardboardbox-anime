namespace CardboardBox.Manga.MangaDex
{
	using Models;

	public interface IMangaDexService
	{
		Task<MangaDexRoot<MangaDexManga>?> Manga(string id, string[]? includes = null);

		Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, ChaptersFilter? filter);

		Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, int limit = 500, int offset = 0);

		Task<MangaDexPages?> Pages(string id);
	}

	public class MangaDexService : IMangaDexService
	{
		private readonly IApiService _api;

		public MangaDexService(IApiService api)
		{
			_api = api;
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

		public Task<MangaDexCollection<MangaDexChapter>?> Chapters(string id, ChaptersFilter? filter)
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

		public Task<MangaDexPages?> Pages(string id)
		{
			var url = $"https://api.mangadex.org/at-home/server/{id}?forcePort443=false";
			return _api.Get<MangaDexPages>(url);
		}
	}
}
