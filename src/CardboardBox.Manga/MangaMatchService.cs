
using MangaDexSharp;
using MManga = MangaDexSharp.Manga;

namespace CardboardBox.Manga;

using Anime.Database;
using CardboardBox.Redis;
using MangaDex;
using Match;

public interface IMangaMatchService
{
	Task<MatchMeta<MangaMetadata>[]> Search(string image);
	Task<MatchMeta<MangaMetadata>[]> Search(Stream stream, string filename);
	Task<MatchMeta<MangaMetadata>[]> Search(MemoryStream stream, string filename);

	Task IndexLatest();

	Task Queue(IndexRequest request);

	Task<bool> IndexPageProxy(string image, MangaMetadata metadata, string? referer, bool noCache = false);

	Task<bool> IndexPage(string url, MangaMetadata metadata);

	Task<(DbMangaChapter chapter, DbManga manga)> Convert(Chapter chapter, MManga manga, string[] pages);

	Task<bool> IndexManga(string id);

	Task<int> FixCoverArt();
}

public class MangaMatchService : IMangaMatchService
{
	private const string DEFAULT_LANG = "en";

	private readonly IMatchApiService _api;
	private readonly IMangaDexService _md;
	private readonly ILogger _logger;
	private readonly IMangaCacheDbService _db;
	private readonly IMangaService _manga;
	private readonly IRedisService _redis;
	private readonly IConfiguration _config;

	public string QueueName => $"{_config["Redis:Queue:Name"]}:queue";

	public MangaMatchService(
		IMatchApiService api,
		IMangaDexService md,
		ILogger<MangaMatchService> logger,
		IMangaCacheDbService db,
		IMangaService manga,
		IRedisService redis,
		IConfiguration config)
	{
		_api = api;
		_md = md;
		_logger = logger;
		_db = db;
		_manga = manga;
		_redis = redis;
		_config = config;
	}

	public string ProxyUrl(string url, string group = "manga-page", string? referer = null, bool noCache = false)
	{
		var path = WebUtility.UrlEncode(url);
		var uri = $"https://cba-proxy.index-0.com/proxy?path={path}&group={group}";
		if (!string.IsNullOrEmpty(referer))
			uri += $"&referer={WebUtility.UrlEncode(referer)}";
		if (noCache)
			uri += $"&noCache=true";

		return uri;
	}

	public string GenerateId(MangaMetadata data)
	{
		return data.Type switch
		{
			MangaMetadataType.Page => $"page:{data.MangaId}:{data.ChapterId}:{data.Page}",
			MangaMetadataType.Cover => $"cover:{data.MangaId}:{data.Id}",
			_ => $"unknown:{data.Id}"
		};
	}

	public Task<bool> IndexPageProxy(string image, MangaMetadata metadata, string? referer, bool noCache = false)
	{
		var imageUrl = ProxyUrl(image, metadata.Type == MangaMetadataType.Page ? "manga-page" : "manga-cover", referer, noCache);
		return IndexPage(imageUrl, metadata);
	}

	public async Task<bool> IndexPage(string url, MangaMetadata metadata)
	{
		var filename = GenerateId(metadata);
		var result = await _api.Add(url, filename, metadata);

		if (result == null)
		{
			_logger.LogError($"Error occurred while indexing image, the result was null: {metadata.Id} >> {metadata.Source}");
			return false;
		}

		if (!result.Success)
		{
			_logger.LogError($"Error occurred while indexing image, {string.Join(", ", result.Error)}: {metadata.Id} >> {metadata.Source}");
			return false;
		}

		return true;
	}

	public async Task<MatchMeta<MangaMetadata>[]> Search(Task<MatchSearchResults<MangaMetadata>?> task, string name)
	{
		var result = await task;
		if (result == null)
		{
			_logger.LogError($"Error occurred while searching for image: {name}");
			return Array.Empty<MatchMeta<MangaMetadata>>();
		}

		if (!result.Success)
		{
			_logger.LogError($"Error occurred while searching for image, {string.Join(", ", result.Error)}: {name}");
			return Array.Empty<MatchMeta<MangaMetadata>>();
		}

		return result.Result;
	}

	public async Task<MatchMeta<MangaMetadata>[]> Search(Stream stream, string filename)
	{
		var result = _api.Search<MangaMetadata>(stream, filename);
		return await Search(result, filename);
	}

	public async Task<MatchMeta<MangaMetadata>[]> Search(MemoryStream stream, string filename)
	{
		var result = _api.Search<MangaMetadata>(stream, filename);
		return await Search(result, filename);
	}

	public async Task<MatchMeta<MangaMetadata>[]> Search(string image)
	{
		var url = ProxyUrl(image, "external");
		var result = _api.Search<MangaMetadata>(url);
		return await Search(result, image);
	}

	public async Task<bool> IndexManga(string id)
	{
		var latest = await _md.Chapters(new ChaptersFilter
		{
			Manga = id
		});

		if (latest == null || latest.Data.Count == 0)
		{
			_logger.LogWarning("Manga match indexing: No new chapters found.");
			return false;
		}
		try
		{
			await IndexChapterList(latest, true);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An error occurred while indexing manga: {id}", id);
			return false;
		}

		return true;
	}

	public async Task Queue(IndexRequest request)
	{
		await _redis.List<IndexRequest>(QueueName).Append(request);
		await _redis.Publish(QueueName, request);
	}

	public async Task IndexChapterList(ChapterList latest, bool reindex = false)
	{
		await PolyfillCoverArt(latest);

		var chapIds = latest.Data.Select(t => t.Id).ToArray();
		var existings = (await _db.DetermineExisting(chapIds)).ToDictionary(t => t.chapter.SourceId);

		foreach (var chapter in latest.Data)
		{
			var existing = existings.ContainsKey(chapter.Id) ? existings[chapter.Id] : null;
			var manga = GetMangaRel(chapter);

			if (manga == null)
			{
				_logger.LogWarning("Manga match indexing: Couldn't find manga relationship to chapter: " + chapter.Id);
				continue;
			}

			if (existing != null && !reindex) continue;

			if (!string.IsNullOrEmpty(chapter.Attributes.ExternalUrl))
			{
				_logger.LogWarning($"Manga match indexing: External URL detected, skipping: {manga.Attributes.Title.PreferedOrFirst(t => t.Key == "en").Value} ({manga.Id}) >> {chapter.Attributes.Title} ({chapter.Id})");
				continue;
			}

			var (dbChap, dbManga) = await Convert(chapter, manga, Array.Empty<string>());

			await Queue(new IndexRequest
			{
				Type = IndexRequest.TYPE_PAGES,
				MangaId = manga.Id,
				ChapterId = chapter.Id,
			});
			await Queue(new IndexRequest
			{
				Type = IndexRequest.TYPE_COVER,
				MangaId = manga.Id,
				Url = dbManga.Cover,
			});

			_logger.LogInformation("Manga match indexing: Indexed chapter queued >> {MangaTitle} ({MangaSourceId}) >> {ChapterTitle} ({ChapterSourceId})",
				dbManga.Title,
				dbManga.SourceId,
				dbChap.Title,
				dbChap.SourceId);
		}
	}

	public async Task IndexLatest()
	{
		var latest = await _md.ChaptersLatest();
		if (latest == null || latest.Data.Count == 0)
		{
			_logger.LogWarning("Manga match indexing: No new chapters found.");
			return;
		}

		await IndexChapterList(latest);
	}

	public async Task PolyfillCoverArt(ChapterList data)
	{
		var ids = new List<string>();
		foreach(var chapter in data.Data)
		{
			var m = GetMangaRel(chapter);
			if (m == null) continue;

			ids.Add(m.Id);
		}

		var manga = await _md.AllManga(ids.Distinct().ToArray());
		if (manga == null || manga.Data.Count == 0)
			return;

		foreach(var chapter in data.Data)
		{
			foreach(var rel in chapter.Relationships)
			{
				if (rel is not RelatedDataRelationship mr) continue;

				var existing = manga.Data.FirstOrDefault(t => t.Id == mr.Id);
				if (existing == null) continue;

				mr.Attributes = existing.Attributes;
				mr.Relationships = existing.Relationships;
			}
		}
	}

	public MManga? GetMangaRel(Chapter chapter)
	{
		var m = chapter.Relationships.FirstOrDefault(t => t is MManga);
		if (m == null) return null;

		return (MManga)m;
	}

	public async Task<(DbMangaChapter chapter, DbManga manga)> Convert(Chapter chapter, MManga manga, string[] pages)
	{
		var m = await Convert(manga);
		var c = await Convert(chapter, m.Id, pages);
		return (c, m);
	}

	public async Task<DbManga> Convert(MManga manga)
	{
		var nsfwRatings = new[] { "erotica", "suggestive", "pornographic" };
		var title = manga.Attributes.Title.PreferedOrFirst(t => t.Key == DEFAULT_LANG).Value;

		var coverFile = (manga.Relationships.FirstOrDefault(t => t is CoverArtRelationship) as CoverArtRelationship)?.Attributes?.FileName;
		var coverUrl = $"https://mangadex.org/covers/{manga.Id}/{coverFile}";

		var item = new DbManga
		{
			HashId = _manga.GenerateHashId(title),
			Title = title,
			SourceId = manga.Id,
			Provider = "mangadex",
			Url = $"https://mangadex.org/title/{manga.Id}",
			AltTitles = manga.Attributes.AltTitles.SelectMany(t => t.Values).ToArray(),
			Description = manga.Attributes.Description.PreferedOrFirst(t => t.Key == DEFAULT_LANG).Value ?? "No Description Provided",
			Nsfw = nsfwRatings.Contains(manga.Attributes.ContentRating?.ToString() ?? ""),
			Cover = coverUrl,

			Attributes = new[]
				{
					new DbMangaAttribute("Content Rating", manga.Attributes.ContentRating ?.ToString() ?? ""),
					new("Original Language", manga.Attributes.OriginalLanguage),
					new("Status", manga.Attributes.Status ?.ToString() ?? ""),
					new("Publication State", manga.Attributes.State)
				}
				.Concat(manga.Relationships.Select(t => t switch
				{
					PersonRelationship person => new DbMangaAttribute(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name),
					ScanlationGroup group => new DbMangaAttribute("Scanlation Group", group.Attributes.Name),
					_ => new("", "")
				})
				.Where(t => !string.IsNullOrEmpty(t.Name)))
				.ToArray(),

			Tags = manga.Attributes.Tags
				.Select(t => t.Attributes.Name.PreferedOrFirst(t => t.Key == DEFAULT_LANG).Value)
				.ToArray(),

			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		item.Id = await _db.Upsert(item);
		return item;
	}

	public async Task<DbMangaChapter> Convert(Chapter chapter, long mangaId, string[] pages)
	{
		var item = new DbMangaChapter
		{
			Title = chapter.Attributes.Title ?? chapter.Attributes.Chapter ?? "No Title",
			Url = $"https://mangadex.org/chapter/{chapter.Id}",
			SourceId = chapter.Id,
			MangaId = mangaId,
			Ordinal = double.TryParse(chapter.Attributes.Chapter, out var a) ? a : 0,
			Volume = double.TryParse(chapter.Attributes.Volume, out var b) ? b : null,
			ExternalUrl = chapter.Attributes.ExternalUrl,
			Language = chapter.Attributes.TranslatedLanguage,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow,
			Pages = pages,
			Attributes = new[]
				{
					new DbMangaAttribute("Translated Language", chapter?.Attributes?.TranslatedLanguage ?? ""),
					new("Uploader", chapter?.Attributes?.Uploader ?? "")
				}
				.Concat(chapter?.Relationships?.Select(t => t switch
				{
					PersonRelationship person => new DbMangaAttribute(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name),
					ScanlationGroup group => new DbMangaAttribute("Scanlation Group", group.Attributes.Name),
					_ => new("", "")
				})?
				.Where(t => !string.IsNullOrEmpty(t.Name))?
				.ToArray() ?? Array.Empty<DbMangaAttribute>())?
				.ToArray() ?? Array.Empty<DbMangaAttribute>()
		};

		item.Id = await _db.Upsert(item);
		return item;
	}

	public async Task<int> FixCoverArt()
	{
		var badCovers = await _db.BadCoverArt();
		if (badCovers.Length == 0)
		{
			_logger.LogInformation("No bad covers found!");
			return 0;
		}

		int count = 0;
		var chunkCounts = (int)Math.Ceiling((double)badCovers.Length / 100);
		var chunks = badCovers.Select(t => t.SourceId).Distinct().Split(chunkCounts).ToArray();
		foreach (var chunk in chunks)
		{
			var manga = await _md.AllManga(chunk);

			if (manga == null || manga.Data.Count == 0)
			{
				_logger.LogInformation("No bad cover relationships / manga found!");
				continue;
			}

			foreach (var m in manga.Data)
			{
				await Convert(m);
				count++;
			}
		}

		_logger.LogInformation("Bad covers fixed (hopefully?) {count}", count);
		return count;
	}
}

public class MangaMetadata
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty;

	[JsonPropertyName("source")]
	public string Source { get; set; } = string.Empty;

	[JsonPropertyName("type")]
	public MangaMetadataType Type { get; set; }

	[JsonPropertyName("mangaId")]
	public string MangaId { get; set; } = string.Empty;

	[JsonPropertyName("chapterId")]
	public string? ChapterId { get; set; }

	[JsonPropertyName("page")]
	public int? Page { get; set; }
}

public enum MangaMetadataType
{
	Page,
	Cover
}
