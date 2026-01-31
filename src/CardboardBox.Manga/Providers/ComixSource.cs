namespace CardboardBox.Manga.Providers;

public interface IComixSource : IMangaSource
{

}

internal class ComixSource(
	ComixApiService _api,
	ILogger<ComixSource> _logger) : IComixSource
{
	public string HomeUrl => "https://comix.to";

	public string Provider => "comix-to";

	public async Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
	{
		var manga = await _api.Manga(mangaId);
		if (manga is null)
		{
			_logger.LogWarning("Manga not found: {MangaId}", mangaId);
			return null;
		}

		var chapter = await _api.Chapter(chapterId);
		if (chapter is null)
		{
			_logger.LogWarning("Chapter not found: {ChapterId}", chapterId);
			return null;
		}

		var pages = chapter.Result.Images
			.Select(i => i.Url)
			.ToArray();
		return new MangaChapterPages
		{
			Title = chapter.Result.Name,
			Url = $"{HomeUrl}/title/{manga.Result.HashId}-{manga.Result.Slug}/{chapter.Result.ChapterId}-chapter-{chapter.Result.Number}",
			Id = chapter.Result.ChapterId.ToString(),
			Number = chapter.Result.Number,
			Volume = chapter.Result.Volume == 0 ? null : chapter.Result.Volume,
			ExternalUrl = null,
			Attributes = [],
			Pages = pages
		};
	}

	public async Task<Manga?> Manga(string id)
	{
		async IAsyncEnumerable<MangaChapter> GetChapters(Comix<Comix.Manga> manga)
		{
			await foreach (var chapter in _api.AllChapters(id))
			{
				yield return new MangaChapter
				{
					Title = chapter.Name,
					Url = $"{HomeUrl}/title/{manga.Result.HashId}-{manga.Result.Slug}/{chapter.ChapterId}-chapter-{chapter.Number}",
					Id = chapter.ChapterId.ToString(),
					Number = chapter.Number,
					Volume = chapter.Volume == 0 ? null : chapter.Volume,
					ExternalUrl = null,
					Attributes = [],
				};
			}
		};

		var manga = await _api.Manga(id);
		if (manga is null)
		{
			_logger.LogWarning("Manga not found: {MangaId}", id);
			return null;
		}

		return new Manga
		{
			Title = manga.Result.Title,
			Id = manga.Result.HashId,
			Provider = Provider,
			HomePage = $"{HomeUrl}/title/{manga.Result.HashId}-{manga.Result.Slug}",
			Cover = manga.Result.Poster.Large,
			Description = manga.Result.Synopsis,
			AltTitles = manga.Result.AltTitles,
			Tags = [],
			Chapters = await GetChapters(manga).OrderBy(t => t.Number).ToListAsync(),
			Nsfw = manga.Result.IsNsfw,
			Attributes = [],
			Referer = HomeUrl,
			SourceCreated = DateTimeOffset.FromUnixTimeSeconds(manga.Result.CreatedAt).DateTime,
			OrdinalVolumeReset = false,
		};
	}

	public (bool matches, string? part) MatchesProvider(string url)
	{
		string URL = $"{HomeUrl}/title/";
		if (!url.StartsWith(URL, StringComparison.InvariantCultureIgnoreCase))
			return (false, null);

		var parts = url[URL.Length..].Split("-", StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return (false, null);

		return (true, parts.First());
	}
}

internal class ComixApiService(IApiService _api)
{
	public const string BASE_URL = "https://comix.to/api/v2";

	public static string WrapUrl(string url)
	{
		if (url.StartsWith("http"))
			return url;
		return $"{BASE_URL}/{url.TrimStart('/')}";
	}

	public Task<Comix<Comix.Manga>?> Manga(string id)
	{
		return _api.Get<Comix<Comix.Manga>>(WrapUrl($"manga/{id}"));
	}

	public Task<Comix<Comix.Chapter>?> Chapter(string id)
	{
		return _api.Get<Comix<Comix.Chapter>>(WrapUrl($"chapters/{id}"));
	}

	public Task<Comix<Comix.ChapterList>?> Chapters(string id, int page = 1)
	{
		return _api.Get<Comix<Comix.ChapterList>>(WrapUrl($"manga/{id}/chapters?page={page}&limit=100"));
	}

	public async IAsyncEnumerable<Comix.Chapter> AllChapters(string mangaId)
	{
		int page = 1;
		while (true)
		{
			var result = await Chapters(mangaId, page);
			if (result == null || result.Result.Items.Length == 0)
				yield break;

			foreach (var chap in result.Result.Items)
				yield return chap;

			if (page >= result.Result.Pagination.LastPage)
				yield break;

			page++;
		}
	}
}

public class Comix
{
	public partial class Manga
	{
		[JsonPropertyName("manga_id")]
		public long MangaId { get; set; }

		[JsonPropertyName("hash_id")]
		public string HashId { get; set; } = string.Empty;

		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("alt_titles")]
		public string[] AltTitles { get; set; } = [];

		[JsonPropertyName("synopsis")]
		public string Synopsis { get; set; } = string.Empty;

		[JsonPropertyName("slug")]
		public string Slug { get; set; } = string.Empty;

		[JsonPropertyName("rank")]
		public long Rank { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; } = string.Empty;

		[JsonPropertyName("poster")]
		public Poster Poster { get; set; } = new();

		[JsonPropertyName("original_language")]
		public string OriginalLanguage { get; set; } = string.Empty;

		[JsonPropertyName("status")]
		public string Status { get; set; } = string.Empty;

		[JsonPropertyName("final_volume")]
		public long FinalVolume { get; set; }

		[JsonPropertyName("final_chapter")]
		public long FinalChapter { get; set; }

		[JsonPropertyName("has_chapters")]
		public bool HasChapters { get; set; }

		[JsonPropertyName("latest_chapter")]
		public long LatestChapter { get; set; }

		[JsonPropertyName("chapter_updated_at")]
		public long ChapterUpdatedAt { get; set; }

		[JsonPropertyName("start_date")]
		public long StartDate { get; set; }

		[JsonPropertyName("end_date")]
		public string EndDate { get; set; } = string.Empty;

		[JsonPropertyName("created_at")]
		public long CreatedAt { get; set; }

		[JsonPropertyName("updated_at")]
		public long UpdatedAt { get; set; }

		[JsonPropertyName("rated_avg")]
		public double RatedAvg { get; set; }

		[JsonPropertyName("rated_count")]
		public long RatedCount { get; set; }

		[JsonPropertyName("follows_total")]
		public long FollowsTotal { get; set; }

		[JsonPropertyName("links")]
		public Dictionary<string, string> Links { get; set; } = [];

		[JsonPropertyName("is_nsfw")]
		public bool IsNsfw { get; set; }

		[JsonPropertyName("year")]
		public long Year { get; set; }

		[JsonPropertyName("term_ids")]
		public long[] TermIds { get; set; } = [];
	}

	public partial class Poster
	{
		[JsonPropertyName("small")]
		public string Small { get; set; } = string.Empty;

		[JsonPropertyName("medium")]
		public string Medium { get; set; } = string.Empty;

		[JsonPropertyName("large")]
		public string Large { get; set; } = string.Empty;
	}

	public partial class Chapter
	{
		[JsonPropertyName("chapter_id")]
		public long ChapterId { get; set; }

		[JsonPropertyName("manga_id")]
		public long MangaId { get; set; }

		[JsonPropertyName("scanlation_group_id")]
		public long ScanlationGroupId { get; set; }

		[JsonPropertyName("is_official")]
		public long IsOfficial { get; set; }

		[JsonPropertyName("number")]
		public double Number { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("language")]
		public string Language { get; set; } = string.Empty;

		[JsonPropertyName("volume")]
		public double Volume { get; set; }

		[JsonPropertyName("votes")]
		public long Votes { get; set; }

		[JsonPropertyName("created_at")]
		public long CreatedAt { get; set; }

		[JsonPropertyName("updated_at")]
		public long UpdatedAt { get; set; }

		[JsonPropertyName("scanlation_group")]
		public ScanlationGroup ScanlationGroup { get; set; } = new();

		[JsonPropertyName("images")]
		public Image[] Images { get; set; } = [];
	}

	public partial class Image
	{
		[JsonPropertyName("width")]
		public long Width { get; set; }

		[JsonPropertyName("height")]
		public long Height { get; set; }

		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;
	}

	public partial class ScanlationGroup
	{
		[JsonPropertyName("scanlation_group_id")]
		public long ScanlationGroupId { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("slug")]
		public string Slug { get; set; } = string.Empty;
	}

	public class ChapterList
	{
		[JsonPropertyName("items")]
		public Chapter[] Items { get; set; } = [];

		[JsonPropertyName("pagination")]
		public Pagination Pagination { get; set; } = new();
	}

	public partial class Pagination
	{
		[JsonPropertyName("count")]
		public long Count { get; set; }

		[JsonPropertyName("total")]
		public long Total { get; set; }

		[JsonPropertyName("per_page")]
		public long PerPage { get; set; }

		[JsonPropertyName("current_page")]
		public long CurrentPage { get; set; }

		[JsonPropertyName("last_page")]
		public long LastPage { get; set; }

		[JsonPropertyName("from")]
		public long From { get; set; }

		[JsonPropertyName("to")]
		public long To { get; set; }
	}

}

public class Comix<T>
{
	[JsonPropertyName("status")]
	public int Status { get; set; }

	[JsonPropertyName("result")]
	public T Result { get; set; } = default!;
}