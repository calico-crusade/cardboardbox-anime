using MangaDexSharp;

namespace CardboardBox.Manga.Providers;

using MangaDex;
using MManga = MangaDexSharp.Manga;


public interface IMangaDexSource : IMangaSource 
{
	Task<Manga[]> Search(string title);

	Task<Manga[]> Search(MangaFilter filter);

	Task<Manga> Convert(MManga manga, bool getChaps = true);
}

public class MangaDexSource : IMangaDexSource
{
	private const string DEFAULT_LANG = "en";
	public string HomeUrl => "https://mangadex.org";
	public string Provider => "mangadex";

	private readonly IMangaDexService _mangadex;

	public MangaDexSource(IMangaDexService mangadex)
	{
		_mangadex = mangadex;
	}

	public async Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		var chapter = await _mangadex.Chapter(chapterId);
		if (chapter == null) return null;

		var chap = new MangaChapterPages
		{
			Title = chapter.Data.Attributes.Title ?? string.Empty,
			Url = $"{HomeUrl}/chapter/{chapter.Data.Id}",
			Id = chapter.Data.Id ?? string.Empty,
			Number = double.TryParse(chapter.Data.Attributes.Chapter, out var a) ? a : 0,
			Volume = double.TryParse(chapter.Data.Attributes.Volume, out var b) ? b : null,
			ExternalUrl = chapter.Data.Attributes.ExternalUrl,
			Attributes = GetChapterAttributes(chapter.Data).ToList()
		};

		if (chapter.Data.Attributes.Pages == 0) return chap;

		var pages = await _mangadex.Pages(chapterId);
		if (pages == null) return null;

		chap.Pages = pages.Images;
		return chap;
	}

	public async Task<Manga> Convert(MManga manga, bool getChaps = true)
	{
		var id = manga.Id;
		var coverFile = (manga.Relationships.FirstOrDefault(t => t is CoverArtRelationship) as CoverArtRelationship)?.Attributes?.FileName;
		var coverUrl = $"{HomeUrl}/covers/{id}/{coverFile}";

		var chapters = getChaps ? await GetChapters(id, DEFAULT_LANG)
			.OrderBy(t => t.Number)
			.ToListAsync() : new List<MangaChapter>();

		var title = DetermineTitle(manga);
		var nsfwRatings = new[] { "erotica", "suggestive", "pornographic" };

		return new Manga
		{
			Title = title,
			Id = id,
			Provider = Provider,
			HomePage = $"{HomeUrl}/title/{id}",
			Cover = coverUrl,
			Description = manga.Attributes.Description.PreferedOrFirst(t => t.Key == DEFAULT_LANG).Value,
			AltTitles = manga.Attributes.AltTitles.SelectMany(t => t.Values).Distinct().ToArray(),
			Tags = manga
				.Attributes
				.Tags
				.Select(t =>
					t.Attributes
					 .Name
					 .PreferedOrFirst(t => t.Key == DEFAULT_LANG)
					 .Value).ToArray(),
			Chapters = chapters,
			Nsfw = nsfwRatings.Contains(manga.Attributes.ContentRating?.ToString() ?? ""),
			Attributes = GetMangaAttributes(manga).ToList(),
			SourceCreated = manga.Attributes.CreatedAt,
			OrdinalVolumeReset = manga.Attributes.ChapterNumbersResetOnNewVolume,
		};
	}

	public Task<Manga[]> Search(string title)
	{
		var filter = new MangaFilter
		{
			Title = title,
			Order = new()
			{
				[MangaFilter.OrderKey.relevance] = OrderValue.asc
			}
		};
		return Search(filter);
	}

	public async Task<Manga[]> Search(MangaFilter filter)
	{
		var results = await _mangadex.Search(filter);
		if (results == null || results.Data == null || results.Data.Count == 0) 
			return Array.Empty<Manga>();

		return await results.Data.Select(t => Convert(t, false)).WhenAll();
	}

	public async Task<Manga?> Manga(string id)
	{
		var manga = await _mangadex.Manga(id);
		if (manga == null || manga.Data == null) return null;

		return await Convert(manga.Data);
	}

	public string DetermineTitle(MManga manga)
	{
		var title = manga.Attributes.Title.PreferedOrFirst(t => t.Key.ToLower() == DEFAULT_LANG);
		if (title.Key.ToLower() == DEFAULT_LANG) return title.Value;

		var prefered = manga.Attributes.AltTitles.FirstOrDefault(t => t.Keys.Contains(DEFAULT_LANG));
		if (prefered != null)
			return prefered.PreferedOrFirst(t => t.Key.ToLower() == DEFAULT_LANG).Value;

		return title.Value;
	}

	public async IAsyncEnumerable<MangaChapter> GetChapters(string id, params string[] languages)
	{
		var filter = new MangaFeedFilter { TranslatedLanguage = languages };
		while(true)
		{
			var chapters = await _mangadex.Chapters(id, filter);
			if (chapters == null) yield break;

			var sortedChapters = chapters
				.Data
				.GroupBy(t => t.Attributes.Chapter + t.Attributes.Volume)
				.Select(t => t.PreferedOrFirst(t => t.Attributes.TranslatedLanguage == DEFAULT_LANG))
				.Where(t => t != null)
				.Select(t => new MangaChapter
                {
					Title = t?.Attributes.Title ?? string.Empty,
					Url = $"{HomeUrl}/chapter/{t?.Id}",
					Id = t?.Id ?? string.Empty,
					Number = double.TryParse(t?.Attributes.Chapter, out var a) ? a : 0,
					Volume = double.TryParse(t?.Attributes.Volume, out var b) ? b : null,
					ExternalUrl = t?.Attributes.ExternalUrl,
					Attributes = GetChapterAttributes(t).ToList()
				})
				.OrderBy(t => t.Volume)
				.OrderBy(t => t.Number);

			foreach (var chap in sortedChapters)
				yield return chap;

			int current = chapters.Offset + chapters.Limit;
			if (chapters.Total <= current) yield break;

			filter.Offset = current;
		}
	}

	public IEnumerable<MangaAttribute> GetChapterAttributes(Chapter? chapter)
	{
		if (chapter == null) yield break;

		yield return new MangaAttribute("Translated Language", chapter.Attributes.TranslatedLanguage);

		if (!string.IsNullOrEmpty(chapter.Attributes.Uploader))
			yield return new MangaAttribute("Uploader", chapter.Attributes.Uploader);

		foreach(var relationship in chapter.Relationships)
		{
			switch (relationship)
			{
				case PersonRelationship per:
					yield return new MangaAttribute(per.Type == "author" ? "Author" : "Artist", per.Attributes.Name);
					break;
				case ScanlationGroup grp:
					if (!string.IsNullOrEmpty(grp.Attributes.Name))
						yield return new MangaAttribute("Scanlation Group", grp.Attributes.Name);
					if (!string.IsNullOrEmpty(grp.Attributes.Website))
						yield return new MangaAttribute("Scanlation Link", grp.Attributes.Website);
					if (!string.IsNullOrEmpty(grp.Attributes.Twitter))
						yield return new MangaAttribute("Scanlation Twitter", grp.Attributes.Twitter);
					if (!string.IsNullOrEmpty(grp.Attributes.Discord))
						yield return new MangaAttribute("Scanlation Discord", grp.Attributes.Discord);
					break;
			}
		}
	}

	public IEnumerable<MangaAttribute> GetMangaAttributes(MManga? manga)
	{
		if (manga == null) yield break;

		if (manga.Attributes.ContentRating != null)
			yield return new("Content Rating", manga.Attributes.ContentRating?.ToString() ?? "");

		if (!string.IsNullOrEmpty(manga.Attributes.OriginalLanguage))
			yield return new("Original Language", manga.Attributes.OriginalLanguage);

		if (manga.Attributes.Status != null)
			yield return new("Status", manga.Attributes.Status?.ToString() ?? "");

		if (!string.IsNullOrEmpty(manga.Attributes.State))
			yield return new("Publication State", manga.Attributes.State);

		foreach(var rel in manga.Relationships)
		{
			switch(rel)
			{
				case PersonRelationship person:
					yield return new(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name);
					break;
				case ScanlationGroup group:
					yield return new("Scanlation Group", group.Attributes.Name);
					break;
			}
		}
	}

	public (bool matches, string? part) MatchesProvider(string url)
	{
		string URL = $"{HomeUrl}/title/";
        if (!url.StartsWith(URL, StringComparison.InvariantCultureIgnoreCase))
			return (false, null);

		var parts = url[URL.Length..].Split("/", StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return (false, null);

		return (true, parts.First());
	}
}
