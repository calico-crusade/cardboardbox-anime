using System.Net;

namespace CardboardBox.Manga
{
	using Anime.Database;
	using MangaDex;
	using MangaDex.Models;
	using Match;

	public interface IMangaMatchService
	{
		Task<MatchMeta<MangaMetadata>[]> Search(string image);

		Task IndexLatest();
	}

	public class MangaMatchService : IMangaMatchService
	{
		private const string DEFAULT_LANG = "en";

		private readonly IMatchApiService _api;
		private readonly IMangaDexService _md;
		private readonly ILogger _logger;
		private readonly IMangaCacheDbService _db;
		private readonly IMangaService _manga;

		public MangaMatchService(
			IMatchApiService api,
			IMangaDexService md,
			ILogger<MangaMatchService> logger,
			IMangaCacheDbService db,
			IMangaService manga)
		{
			_api = api;
			_md = md;
			_logger = logger;
			_db = db;
			_manga = manga;
		}

		public string ProxyUrl(string url, string group = "manga-page", string? referer = null)
		{
			var path = WebUtility.UrlEncode(url);
			var uri = $"https://cba-proxy.index-0.com/proxy?path={path}&group={group}";
			if (!string.IsNullOrEmpty(referer))
				uri += $"&referer={WebUtility.UrlEncode(referer)}";

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

		public async Task<bool> IndexPage(string image, MangaMetadata metadata, string? referer)
		{
			var filename = GenerateId(metadata);
			var imageUrl = ProxyUrl(image, metadata.Type == MangaMetadataType.Page ? "manga-page" : "manga-cover", referer);

			var result = await _api.Add(imageUrl, filename, metadata);
			
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

		public async Task<MatchMeta<MangaMetadata>[]> Search(string image)
		{
			var url = ProxyUrl(image, "external");
			var result = await _api.Search<MangaMetadata>(url);

			if (result == null)
			{
				_logger.LogError($"Error occurred while searching for image: {image}");
				return Array.Empty<MatchMeta<MangaMetadata>>();
			}

			if (!result.Success)
			{
				_logger.LogError($"Error occurred while searching for image, {string.Join(", ", result.Error)}: {image}");
				return Array.Empty<MatchMeta<MangaMetadata>>();
			}

			return result.Result;
		}

		public async Task IndexLatest()
		{
			var latest = await _md.ChaptersLatest();
			if (latest == null || latest.Data.Count == 0)
			{
				_logger.LogWarning("Manga match indexing: No new chapters found.");
				return;
			}

			var chapIds = latest.Data.Select(t => t.Id).ToArray();
			var existings = (await _db.DetermineExisting(chapIds)).ToDictionary(t => t.chapter.SourceId);

			int pageRequests = 0;
			foreach(var chapter in latest.Data)
			{
				var existing = existings.ContainsKey(chapter.Id) ? existings[chapter.Id] : null;
				var manga = GetMangaRel(chapter);

				if (manga == null)
				{
					_logger.LogWarning("Manga match indexing: Couldn't find manga relationship to chapter: " + chapter.Id);
					continue;
				}

				if (existing != null)
				{
					await ProcessUpdates(chapter, manga, existing);
					continue;
				}

				if (pageRequests >= 35)
				{
					_logger.LogDebug($"Manga match indexing: Delaying indexing due to rate-limits >> {manga.Attributes.Title.PreferedOrFirst(t => t.Key == "en").Value} ({manga.Id}) >> {chapter.Attributes.Title ?? chapter.Attributes.Chapter} ({chapter.Id})");
					await Task.Delay(60 * 1000);
					pageRequests = 0;
				}

				if (!string.IsNullOrEmpty(chapter.Attributes.ExternalUrl))
				{
					_logger.LogWarning($"Manga match indexing: External URL detected, skipping: {manga.Attributes.Title.PreferedOrFirst(t => t.Key == "en").Value} ({manga.Id}) >> {chapter.Attributes.Title} ({chapter.Id})");
					continue;
				}


				var pages = await _md.Pages(chapter.Id);
				pageRequests++;
				if (pages == null || pages.Images.Length == 0)
				{
					_logger.LogWarning("Manga match indexing: Couldn't find any pages for chapter: " + chapter.Id);
					continue;
				}

				var (dbChap, dbManga) = await Convert(chapter, manga, pages.Images);

				for(var i = 0; i < dbChap.Pages.Length; i++)
				{
					var url = dbChap.Pages[i]; 
					var meta = new MangaMetadata
					{
						Id = url.MD5Hash(),
						Source = "mangadex",
						Url = url,
						Type = MangaMetadataType.Page,
						MangaId = manga.Id,
						ChapterId = chapter.Id,
						Page = i + 1,
						
					};

					await IndexPage(url, meta, dbManga.Referer);
				}

				_logger.LogDebug($"Manga match indexing: Indexed chapter >> {dbManga.Title} ({dbManga.SourceId}) >> {dbChap.Title} ({dbChap.SourceId})");
			}
		}

		public MangaDexManga? GetMangaRel(MangaDexChapter chapter)
		{
			var m = chapter.Relationships.FirstOrDefault(t => t is MangaDexManga);
			if (m == null) return null;

			return (MangaDexManga)m;
		}

		public async Task ProcessUpdates(MangaDexChapter chapter, MangaDexManga manga, MangaCache cache)
		{

		}

		public async Task<(DbMangaChapter chapter, DbManga manga)> Convert(MangaDexChapter chapter, MangaDexManga manga, string[] pages)
		{
			var m = await Convert(manga);
			var c = await Convert(chapter, m.Id, pages);
			return (c, m);
		}

		public async Task<DbManga> Convert(MangaDexManga manga)
		{
			var nsfwRatings = new[] { "erotica", "suggestive", "pornographic" };
			var title = manga.Attributes.Title.PreferedOrFirst(t => t.Key == DEFAULT_LANG).Value;
			var item = new DbManga
			{
				HashId = _manga.GenerateHashId(title),
				Title = title,
				SourceId = manga.Id,
				Provider = "mangadex",
				Url = $"https://mangadex.org/title/{manga.Id}",
				AltTitles = manga.Attributes.AltTitles.SelectMany(t => t.Values).ToArray(),
				Description = manga.Attributes.Description.PreferedOrFirst(t => t.Key == DEFAULT_LANG).Value ?? "No Description Provided",
				Nsfw = nsfwRatings.Contains(manga.Attributes.ContentRating),

				Attributes = new[]
					{
						new DbMangaAttribute("Content Rating", manga.Attributes.ContentRating),
						new("Original Language", manga.Attributes.OriginalLanguage),
						new("Status", manga.Attributes.Status),
						new("Publication State", manga.Attributes.State)
					}
					.Concat(manga.Relationships.Select(t => t switch
					{
						PersonRelationship person => new DbMangaAttribute(person.Type == "author" ? "Author" : "Artist", person.Attributes.Name),
						ScanlationGroupRelationship group => new DbMangaAttribute("Scanlation Group", group.Attributes.Name),
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

		public async Task<DbMangaChapter> Convert(MangaDexChapter chapter, long mangaId, string[] pages)
		{
			var item = new DbMangaChapter
			{
				Title = string.IsNullOrEmpty(chapter.Attributes.Title) ? chapter.Attributes.Chapter : chapter.Attributes.Title,
				Url = $"https://mangadex.org/chapter/{chapter.Id}",
				SourceId = chapter.Id,
				MangaId = mangaId,
				Ordinal = double.TryParse(chapter.Attributes.Chapter, out var a) ? a : 0,
				Volume = double.TryParse(chapter.Attributes.Volume, out var b) ? b : null,
				ExternalUrl = chapter.Attributes.ExternalUrl,
				Language = chapter.Attributes.TranslatedLanguage,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				Pages = pages
			};

			item.Id = await _db.Upsert(item);
			return item;
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
}
