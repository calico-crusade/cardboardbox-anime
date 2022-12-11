using F23.StringSimilarity;

namespace CardboardBox.Manga
{
	using Anime.Database;
	using Providers;

	public interface IMangaSearchService
	{
		Task<ImageSearchResults> Search(string image);
	}

	public class MangaSearchService : IMangaSearchService
	{
		private readonly IGoogleVisionService _vision;
		private readonly IMangaMatchService _match;
		private readonly IMangaDexSource _md;
		private readonly ILogger _logger;
		private readonly IMangaCacheDbService _db;

		public MangaSearchService(
			IGoogleVisionService vision, 
			IMangaMatchService match,
			IMangaDexSource md,
			ILogger<MangaSearchService> logger,
			IMangaCacheDbService db)
		{
			_vision = vision;
			_match = match;
			_md = md;
			_logger = logger;
			_db = db;
		}

		public async Task<ImageSearchResults> Search(string image)
		{
			var results = new ImageSearchResults();

			await Task.WhenAll(
				HandleFallback(image, results), 
				HandleVision(image, results));

			DetermineBestGuess(results);

			return results;
		}

		public void DetermineBestGuess(ImageSearchResults results)
		{
			if (results.Match.Count == 0 && results.Vision.Count == 0) return;

			var exact = results.Match.FirstOrDefault(t => t.ExactMatch)?.Manga;
			if (exact != null)
			{
				results.BestGuess = exact;
				return;
			}

			exact = results.Vision.FirstOrDefault(t => t.ExactMatch)?.Manga;
			if (exact != null)
			{
				results.BestGuess = exact;
				return;
			}

			var bestFall = results.Match.OrderByDescending(t => t.Score).FirstOrDefault();
			var bestVisi = results.Vision.OrderByDescending(t => t.Score).FirstOrDefault();

			if (bestFall == null && bestVisi != null)
			{
				results.BestGuess = bestVisi.Manga;
				return;
			}

			if (bestFall != null && bestVisi == null)
			{
				results.BestGuess = bestFall.Manga;
				return;
			}

			if (bestFall == null || bestVisi == null) return;

			results.BestGuess = bestVisi.Score * 100 > bestFall.Score ? bestVisi.Manga : bestFall.Manga;
		}

		#region Fallback
		public async Task HandleFallback(string image, ImageSearchResults output)
		{
			var results = await _match.Search(image);
			if (results.Length == 0) return;

			var ids = results
				.Select(t => t.Metadata?.MangaId ?? "")
				.Where(t => !string.IsNullOrEmpty(t))
				.ToArray();

			var manga = await _db.ByIds(ids);

			foreach(var res in results)
			{
				var m = manga.FirstOrDefault(t => t.SourceId == res.Metadata?.MangaId);
				var trimmed = m == null ? null : (TrimmedManga)m;

				var fallback = new FallbackResult
				{
					Score = res.Score,
					ExactMatch = res.Score >= 100,
					Manga = trimmed,
					Metadata = res.Metadata
				};

				output.Match.Add(fallback);
			}
		}
		#endregion

		#region Vision
		public async Task HandleVision(string image, ImageSearchResults output)
		{
			var vision = await _vision.ExecuteVisionRequest(image);
			if (vision == null) return;

			for(var i = 0; i < vision.WebPages.Length && i < 3; i++)
			{
				var (url, title) = vision.WebPages[i];
				var filtered = PurgeVisionTitle(title);

				if (string.IsNullOrEmpty(filtered)) continue;

				var results = await SearchMangaDex(filtered, url, title).ToArrayAsync();

				output.Vision.AddRange(results);
				if (results.Any(t => t.ExactMatch)) return;
			}
		}

		public async IAsyncEnumerable<VisionResult> SearchMangaDex(string title, string url, string originalTitle)
		{
			var search = Array.Empty<Manga>();

			try
			{
				search = await _md.Search(title);
				if (search == null || search.Length == 0) yield break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error occurred while searching mangadex: {title}");
				yield break;
			}

			var sort = Rank(title, search)
				.OrderByDescending(t => t.Compute);

			int count = 0;
			foreach(var (compute, match, manga) in sort)
			{
				if (count >= 3) yield break;

				var output = new VisionResult
				{
					Url = url,
					Title = originalTitle,
					FilteredTitle = title,
					Score = compute,
					ExactMatch = compute > 1,
					Manga = (TrimmedManga)manga
				};

				yield return output;
				count++;
			}
		}

		public IEnumerable<(double Compute, bool Main, Manga Manga)> Rank(string title, Manga[] manga)
		{
			var check = new NormalizedLevenshtein();

			foreach (var m in manga)
			{
				var mt = PurgeVisionTitle(m.Title);
				if (mt == title)
				{
					yield return (1.2, true, m);
					continue;
				}

				yield return (check.Distance(title, mt), true, m);

				foreach (var t in m.AltTitles)
				{
					var mtt = PurgeVisionTitle(t);
					if (mtt == title)
					{
						yield return (1.1, false, m);
						continue;
					}

					yield return (check.Distance(title, mtt), false, m);
				}
			}
		}

		public string PurgeVisionTitle(string title)
		{
			var regexPurgers = new[]
			{
				("manga", "manga[a-z]{1,}\\b")
			};

			var purgers = new[]
			{
				("chapter", new[] { "chapter" }),
				("chap", new[] { "chap" }),
				("read", new[] { "read" }),
				("online", new[] { "online" }),
				("manga", new[] { "manga" }),
				("season", new[] { "season" }),
				("facebook", new[] { "facebook" })
			};

			title = title.ToLower();

			if (title.Contains("<"))
			{
				var doc = new HtmlDocument();
				doc.LoadHtml(title);
				title = doc.DocumentNode.InnerText;
			}

			if (title.Contains("&")) title = WebUtility.HtmlDecode(title);

			foreach (var (text, regex) in regexPurgers)
				if (title.Contains(text))
					title = Regex.Replace(title, regex, string.Empty);

			foreach (var (text, replacers) in purgers)
				if (title.Contains(text))
					foreach (var regex in replacers)
						title = title.Replace(regex, "").Trim();

			title = new string(title
				.Select(t => !char.IsPunctuation(t) &&
					!char.IsNumber(t) &&
					!char.IsSymbol(t) ? t : ' ').ToArray());

			while (title.Contains("  "))
				title = title.Replace("  ", " ").Trim();

			return title;
		}
		#endregion
	}

	public class ImageSearchResults
	{
		[JsonPropertyName("vision")]
		public List<VisionResult> Vision { get; set; } = new();

		[JsonPropertyName("match")]
		public List<FallbackResult> Match { get; set; } = new();

		[JsonPropertyName("bestGuess")]
		public TrimmedManga? BestGuess { get; set; }

		[JsonIgnore]
		public bool Success => Vision.Count > 0 || Match.Count > 0;
	}

	public class TrimmedManga
	{
		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("id")]
		public string Id { get; set; } = string.Empty;

		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;

		[JsonPropertyName("source")]
		public string Source { get; set; } = string.Empty;

		[JsonPropertyName("nsfw")]
		public bool Nsfw { get; set; }

		[JsonPropertyName("cover")]
		public string Cover { get; set; } = string.Empty;

		[JsonPropertyName("tags")]
		public string[] Tags { get; set; } = Array.Empty<string>();

		public static implicit operator TrimmedManga(Manga manga)
		{
			return new TrimmedManga
			{
				Title = manga.Title,
				Id = manga.Id,
				Url = $"https://mangadex.org/title/" + manga.Id,
				Description = manga.Description,
				Tags = manga.Tags,
				Cover = manga.Cover,
				Nsfw = manga.Nsfw,
				Source = manga.Provider
			};
		}

		public static implicit operator TrimmedManga(DbManga manga)
		{
			return new TrimmedManga
			{
				Title = manga.Title,
				Id = manga.SourceId,
				Url = $"https://mangadex.org/title/" + manga.SourceId,
				Description = manga.Description,
				Tags = manga.Tags,
				Cover = manga.Cover,
				Nsfw = manga.Nsfw,
				Source = manga.Provider
			};
		}
	}

	public abstract class BaseResult
	{
		[JsonPropertyName("score")]
		public double Score { get; set; }

		[JsonPropertyName("exactMatch")]
		public bool ExactMatch { get; set; }

		[JsonPropertyName("manga")]
		public TrimmedManga? Manga { get; set; }
	}

	public class VisionResult : BaseResult
	{
		[JsonPropertyName("url")]
		public string Url { get; set; } = string.Empty;

		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("filteredTitle")]
		public string FilteredTitle { get; set; } = string.Empty;
	}

	public class FallbackResult : BaseResult
	{
		[JsonPropertyName("metadata")]
		public MangaMetadata? Metadata { get; set; }
	}
}
