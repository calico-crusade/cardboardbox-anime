using WeebDexSharp;
using WeebDexSharp.Models;
using WeebDexSharp.Models.Types;

namespace CardboardBox.Manga.Providers;

public interface IWeebDexSource : IMangaSource
{

}


internal class WeebDexSource(
	IWeebDex _api,
	ILogger<WeebDexSource> _logger) : IWeebDexSource
{
	private const string DEFAULT_LANG = "en";
	public string HomeUrl => "https://weebdex.org";
	public string Provider => "weebdex";

	public async Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		var result = await _api.Chapters.Get(chapterId);
		if (!result.Succeeded)
		{
			_logger.LogWarning("Failed to get chapter {ChapterId}: {Error}", chapterId, result.MetaData.Response.ReasonPhrase);
			return null;
		}

		return Convert(result.Data);
	}

	public async Task<Manga?> Manga(string id)
	{
		var manga = await _api.Manga.Get(id);
		if (manga.Succeeded)
			return await Convert(manga.Data);

		_logger.LogWarning("Failed to get manga {MangaId}: {Error}", id, manga.MetaData.Response.ReasonPhrase);
		return null;
	}

	public async Task<Manga> Convert(WdManga manga, bool getChaps = true)
	{
		IEnumerable<MangaAttribute> Attributes()
		{
			yield return new("Content Rating", manga.Rating.ToString());
			yield return new("Original Language", manga.Language);
			yield return new("Status", manga.Status.ToString());
			yield return new("Publication State", manga.State.ToString());
			yield return new("Demographic", manga.Demographic.ToString());

			foreach(var author in manga.Relationships.Authors)
				yield return new("Author", author.Name);
			foreach(var artist in manga.Relationships.Artists)
				yield return new("Artist", artist.Name);
			foreach(var group in manga.Relationships.AvailableGroups)
				yield return new("Scanlation Group", group.Name);
			foreach(var link in manga.Relationships.Links)
				yield return new("Link: " + link.Key, link.Value);
		}

		var id = manga.Id;
		var cover = manga.CoverImageUrl;

		var title = DetermineTitle(manga);
		var nsfwRatings = new[] { ContentRating.Erotica, ContentRating.Suggestive, ContentRating.Pornographic };

		var chapters = getChaps 
			? await GetChapters(id, DEFAULT_LANG).OrderBy(t => t.Number).ToListAsync() 
			: [];

		return new Manga
		{
			Title = title,
			Id = id,
			Provider = Provider,
			HomePage = $"{HomeUrl}/title/{id}",
			Cover = cover,
			Description = manga.Description ?? string.Empty,
			AltTitles = [.. manga.AltTitles.SelectMany(t => t.Value).Distinct()],
			Tags = [.. manga.Relationships.Tags.Select(t => t.Name)],
			Chapters = chapters,
			Nsfw = nsfwRatings.Contains(manga.Rating),
			Attributes = [..Attributes()],
			SourceCreated = manga.CreatedAt.DateTime,
			OrdinalVolumeReset = false
		};
	}

	public MangaChapter Convert(WdChapterPartial chapter)
	{
		IEnumerable<MangaAttribute> Attributes()
		{
			yield return new("Translated Language", chapter.Language);

			if (!string.IsNullOrWhiteSpace(chapter.Relationships.Uploader?.Name))
				yield return new("Uploader", chapter.Relationships.Uploader.Name);

			foreach(var grp in chapter.Relationships.Groups)
				yield return new MangaAttribute("Group", grp.Name);
		}

		return new()
		{
			Title = chapter.Title,
			Url = $"{HomeUrl}/chapter/{chapter.Id}/1",
			Id = chapter.Id,
			Number = double.TryParse(chapter.Chapter, out var num) ? num : 0,
			Volume = double.TryParse(chapter.Volume, out var vol) ? vol : null,
			ExternalUrl = null,
			Attributes = [..Attributes()]
		};
	}

	public MangaChapterPages Convert(WdChapter chapter)
	{
		IEnumerable<MangaAttribute> Attributes()
		{
			yield return new("Translated Language", chapter.Language);

			if (!string.IsNullOrWhiteSpace(chapter.Relationships.Uploader?.Name))
				yield return new("Uploader", chapter.Relationships.Uploader.Name);

			foreach (var grp in chapter.Relationships.Groups)
				yield return new MangaAttribute("Group", grp.Name);
		}

		return new()
		{
			Title = chapter.Title,
			Url = $"{HomeUrl}/chapter/{chapter.Id}/1",
			Id = chapter.Id,
			Number = double.TryParse(chapter.Chapter, out var num) ? num : 0,
			Volume = double.TryParse(chapter.Volume, out var vol) ? vol : null,
			ExternalUrl = null,
			Attributes = [.. Attributes()],
			Pages = [..chapter.ImageUrls.Select(t => t.Name)]
		};
	}

	public static string DetermineTitle(WdManga manga)
	{
		var title = manga.AltTitles.PreferedOrFirst(t => t.Key == DEFAULT_LANG);
		if (title.Key.Equals(DEFAULT_LANG, StringComparison.OrdinalIgnoreCase) &&
			title.Value.Length > 0)
			return title.Value.First();

		return manga.Title;
	}

	public async IAsyncEnumerable<MangaChapter> GetChapters(string id, params string[] languages)
	{
		var filter = new WdMangaChapterFilter
		{
			TranslatedLanguagesInclude = languages,
			Page = 1,
			Limit = 500
		};
		while(true)
		{
			var chapters = await _api.Manga.Chapters(id, filter);
			if (!chapters.Succeeded)
			{
				_logger.LogWarning("Failed to get chapters for manga {MangaId}: {Error}", id, chapters.MetaData.Response.ReasonPhrase);
				yield break;
			}

			foreach(var chap in chapters.Data)
				yield return Convert(chap);

			int current = (chapters.Page - 1) * chapters.Limit + chapters.Data.Length;
			if (chapters.Total <= current) yield break;

			filter.Page++;
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
