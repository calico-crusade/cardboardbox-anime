namespace CardboardBox.Manga.Providers;

using MangaDex;
using MangaDex.Models;

public interface IMangaDexSource : IMangaSource 
{
	Task<Manga[]> Search(string title);

	Task<Manga[]> Search(MangaFilter filter);

	Task<Manga> Convert(MangaDexManga manga, bool getChaps = true);
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
			Attributes = new[]
				{
					new MangaAttribute("Translated Language", chapter.Data.Attributes.TranslatedLanguage),
					new("Uploader", chapter.Data.Attributes.Uploader ?? "")
				}
				.Concat(chapter.Data.Relationships.Select(t => t switch
				{
					PersonRelationship person => new MangaAttribute(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name),
					ScanlationGroupRelationship group => new MangaAttribute("Scanlation Group", group.Attributes.Name),
					_ => new("", "")
				})?
				.Where(t => !string.IsNullOrEmpty(t.Name))?
				.ToArray() ?? Array.Empty<MangaAttribute>())?
				.ToList() ?? new()
		};

		if (chapter.Data.Attributes.Pages == 0) return chap;

		var pages = await _mangadex.Pages(chapterId);
		if (pages == null) return null;

		chap.Pages = pages.Images;
		return chap;
	}

	public async Task<Manga> Convert(MangaDexManga manga, bool getChaps = true)
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
			Nsfw = nsfwRatings.Contains(manga.Attributes.ContentRating),
			Attributes = new[]
				{
					new MangaAttribute("Content Rating", manga.Attributes.ContentRating),
					new("Original Language", manga.Attributes.OriginalLanguage),
					new("Status", manga.Attributes.Status),
					new("Publication State", manga.Attributes.State)
				}
				.Concat(manga.Relationships.Select(t => t switch
				{
					PersonRelationship person => new MangaAttribute(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name),
					ScanlationGroupRelationship group => new MangaAttribute("Scanlation Group", group.Attributes.Name),
					_ => new("", "")
				})
				.Where(t => !string.IsNullOrEmpty(t.Name)))
				.ToList()
		};
	}

	public Task<Manga[]> Search(string title)
	{
		var filter = new MangaFilter
		{
			Title = title,
			Order = new()
			{
				[MangaFilter.OrderKey.relevance] = MangaFilter.OrderValue.asc
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

	public string DetermineTitle(MangaDexManga manga)
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
		var filter = new ChaptersFilter { TranslatedLanguage = languages };
		while(true)
		{
			var chapters = await _mangadex.Chapters(id, filter);
			if (chapters == null) yield break;

			var sortedChapters = chapters
				.Data
				.GroupBy(t => t.Attributes.Chapter)
				.Select(t => t.PreferedOrFirst(t => t.Attributes.TranslatedLanguage == DEFAULT_LANG))
				.Where(t => t != null)
				.Select(t =>
				{
					return new MangaChapter
					{
						Title = t?.Attributes.Title ?? string.Empty,
						Url = $"{HomeUrl}/chapter/{t?.Id}",
						Id = t?.Id ?? string.Empty,
						Number = double.TryParse(t?.Attributes.Chapter, out var a) ? a : 0,
						Volume = double.TryParse(t?.Attributes.Volume, out var b) ? b : null,
						ExternalUrl = t?.Attributes.ExternalUrl,
						Attributes = new[]
						{
							new MangaAttribute("Translated Language", t?.Attributes?.TranslatedLanguage ?? ""),
							new("Uploader", t?.Attributes?.Uploader ?? "")
						}
						.Concat(t?.Relationships?.Select(t => t switch
						{
							PersonRelationship person => new MangaAttribute(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name),
							ScanlationGroupRelationship group => new MangaAttribute("Scanlation Group", group.Attributes.Name),
							_ => new("", "")
						})?
						.Where(t => !string.IsNullOrEmpty(t.Name))?
						.ToArray() ?? Array.Empty<MangaAttribute>())?
						.ToList() ?? new()
					};
				})
				.OrderBy(t => t.Number);

			foreach (var chap in sortedChapters)
				yield return chap;

			int current = chapters.Offset + chapters.Limit;
			if (chapters.Total <= current) yield break;

			filter.Offset = current;
		}
	}

	public (bool matches, string? part) MatchesProvider(string url)
	{
		var regex = new Regex("https://mangadex.org/title/(.*?)(/(.*?))?");
		if (!regex.IsMatch(url)) return (false, null);

		var parts = url.Split('/').Reverse().ToArray();
		
		var last = parts.Skip(1).First();
		if (last == "title")
			last = parts.First();
		return (true, last);
	}
}
