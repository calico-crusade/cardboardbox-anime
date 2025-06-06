﻿using F23.StringSimilarity;

namespace CardboardBox.Manga;

using Anime.Database;
using MangaDex;
using Match;
using Http;
using Providers;
using CardboardBox.Match.SauceNao;
using MangaDexSharp;

public interface IMangaSearchService
{
	Task<ImageSearchResults> Search(MemoryStream stream, string filename);
	Task<ImageSearchResults> Search(string image);
}

public class MangaSearchService : IMangaSearchService
{
	private readonly IGoogleVisionService _vision;
	private readonly IMangaMatchService _match;
	private readonly IMangaDexSource _md;
	private readonly IMangaDexService _mangadex;
	private readonly ILogger _logger;
	private readonly IMangaCacheDbService _db;
	private readonly ISauceNaoApiService _sauce;
	private readonly IApiService _api;

	public MangaSearchService(
		IGoogleVisionService vision, 
		IMangaMatchService match,
		IMangaDexSource md,
		ILogger<MangaSearchService> logger,
		IMangaCacheDbService db,
		IMangaDexService mangadex,
		ISauceNaoApiService sauce,
		IApiService api)
	{
		_vision = vision;
		_match = match;
		_md = md;
		_logger = logger;
		_db = db;
		_mangadex = mangadex;
		_sauce = sauce;
		_api = api;
	}

	public async Task<(MemoryStream? stream, string? filename)> GetImage(string url)
	{
        static string GetUserAgent(string url)
        {
            if (url.ToLower().Contains("mangadex"))
                return "CBA-API-BOT/1.0";

            return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 OPR/106.0.0.0";
        }

        static string GetFileName(string? file, string imageUrl)
        {
            if (!string.IsNullOrEmpty(file)) return file;

            var uri = new Uri(imageUrl);
            return Path.GetFileName(uri.LocalPath);
        }

		try
		{
			var io = new MemoryStream();
			var (stream, _, file, type) = await _api.GetData(url, c => 
			{
				c.Headers.Add("User-Agent", GetUserAgent(url));
			});
			await stream.CopyToAsync(io);
			stream.Dispose();
			io.Position = 0;
			var filename = GetFileName(file, url);
			return (io, filename);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while fetching image: {url}", url);
			return (null, null);
		}
    }

	public async Task<ImageSearchResults> Search(MemoryStream stream, string filename)
	{
		var results = new ImageSearchResults();

		var actions = new Func<MemoryStream, Task>[] 
		{
			(t) => HandleFallback(t, filename, results),
			(t) => HandleSauceNao(t, filename, results),
			(t) => HandleVision(t, filename, results)
		};

		foreach(var action in actions)
		{
			if (AnyMatches(results)) break;

            using var second = new MemoryStream();
            await stream.CopyToAsync(second);

            stream.Position = 0;
            second.Position = 0;

			await action(second);
        }

		DetermineBestGuess(results);

		return results;
	}

	public async Task<ImageSearchResults> Search(string image)
	{
		if (Uri.IsWellFormedUriString(image, UriKind.Absolute))
		{
			var (stream, filename) = await GetImage(image);
			if (stream == null || string.IsNullOrEmpty(filename))
                return new ImageSearchResults();

			var results = new ImageSearchResults();

            using var second = new MemoryStream();
            await stream.CopyToAsync(second);

            stream.Position = 0;
            second.Position = 0;

            await HandleFallback(stream, filename, results);

			if (!AnyMatches(results))
				await HandleSauceNao(image, results);

			if (!AnyMatches(results))
				await HandleVision(second, filename, results);

			DetermineBestGuess(results);

			return results;
		}

		var raw = await _mangadex.Search(image);
		if (raw == null || raw.Data == null || raw.Data.Count == 0) 
			return new ImageSearchResults();

		var data = await raw.Data
			.Select(async t => await _md.Convert(t, false))
			.WhenAll();

		var rank = Rank(image, data)
			.OrderByDescending(t => t.Compute)
			.DistinctBy(t => t.Manga.Id)
			.Select(t => new BaseResult
			{
				Manga = t.Manga,
				ExactMatch = t.Compute > 1,
				Score = t.Compute,
				Source = "title lookup"
			}).ToList();

		return new ImageSearchResults()
		{
			Textual = rank,
			BestGuess = rank.FirstOrDefault()?.Manga
		};
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

		results.BestGuess = bestVisi.Score > bestFall.Score ? bestVisi.Manga : bestFall.Manga;
	}

	public bool AnyMatches(ImageSearchResults results)
	{
		var all = results.All.ToArray();
		if (all.Length == 0) return false;

		var exact = all.Any(t => t.ExactMatch);
		if (exact) return true;

		return all.Any(t => t.Score > 80);
	}

	#region Fallback
	public Task HandleFallback(MemoryStream stream, string filename, ImageSearchResults output)
	{
		return HandleFallback(_match.Search(stream, filename), output);
	}

	public Task HandleFallback(string image, ImageSearchResults output)
	{
		return HandleFallback(_match.Search(image), output);
	}

	public async Task HandleFallback(Task<MatchMeta<MangaMetadata>[]> task, ImageSearchResults output)
	{
		var results = await task;
		if (results.Length == 0) return;

		var ids = results
			.Select(t => t.Metadata?.MangaId ?? "")
			.Where(t => !string.IsNullOrEmpty(t))
			.ToArray();

		var manga = await _db.ByIds(ids);

		foreach (var res in results)
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
	public Task HandleVision(string image, ImageSearchResults output)
	{
		return HandleVision(_vision.ExecuteVisionRequest(image), output);
	}

	public Task HandleVision(MemoryStream stream, string filename, ImageSearchResults output)
	{
		return HandleVision(_vision.ExecuteVisionRequest(stream, filename), output);
	}

	public async Task HandleVision(Task<VisionResults?> task, ImageSearchResults output)
	{
		var vision = await task;
		if (vision == null) return;

		for (var i = 0; i < vision.WebPages.Length && i < 3; i++)
		{
			var (url, title) = vision.WebPages[i];
			var filtered = PurgeVisionTitle(title);

			if (string.IsNullOrEmpty(filtered)) continue;

			var results = await SearchMangaDex(filtered, url, title).ToArrayAsync();

			output.Vision.AddRange(results);
			if (results.Any(t => t.ExactMatch)) break;
		}

		output.Vision = output.Vision
			.OrderByDescending(t => t.Score)
			.DistinctBy(t => t.Url)
			.ToList();
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
				Score = compute * 100,
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

	#region SauceNao
	public async Task HandleSauceNao(Task<Sauce?> fetch, ImageSearchResults results)
	{
		var res = await fetch;
        if (res == null || res.Results.Length == 0) return;

        var mdRes = res
            .Results
            .Where(t => t.Data.ExternalUrls.Any(a => a.ToLower().Contains("mangadex")))
            .Select(t => (t, double.TryParse(t.Header.Similarity, out var sim) ? sim : -1))
            .Where(t => t.Item2 > 70)
            .OrderByDescending(t => t.Item2 != -1)
            .Select(t => t.t)
            .FirstOrDefault();

        if (mdRes == null || string.IsNullOrEmpty(mdRes.Data.Source)) return;

		var manga = new List<MangaDexSharp.Manga>();
		if (!string.IsNullOrEmpty(mdRes.Data.MangaDexId))
		{
			var single = await _mangadex.Chapter(mdRes.Data.MangaDexId);
			if (single is not null && 
				single.Data is not null &&
				single.Data.Relationships.Any(t => t is RelatedDataRelationship))
				manga.Add((single.Data.Relationships.First(t => t is RelatedDataRelationship) as RelatedDataRelationship)!);
		}

		if (manga.Count == 0)
        {
            var title = mdRes.Data.Source;
            var raw = await _mangadex.Search(title);
            if (raw == null || raw.Data == null || raw.Data.Count == 0)
                return;

			manga.AddRange(raw.Data);
        }

        var data = await manga
            .Select(async t => await _md.Convert(t, false))
            .WhenAll();

        var rank = Rank(mdRes.Data.Source, data)
            .OrderByDescending(t => t.Compute)
            .DistinctBy(t => t.Manga.Id)
            .Select(t => new BaseResult
            {
                Manga = t.Manga,
                ExactMatch = t.Compute > 1,
                Score = t.Compute,
                Source = "sauce nao"
            }).ToList();

        results.Textual = rank;
        results.BestGuess = rank.FirstOrDefault()?.Manga;
    }

	public Task HandleSauceNao(string image, ImageSearchResults results)
	{
		return HandleSauceNao(_sauce.Get(image), results);
	}

	public Task HandleSauceNao(Stream stream, string filename, ImageSearchResults results)
    {
        return HandleSauceNao(_sauce.Get(stream, filename), results);
    }
	#endregion
}

public class ImageSearchResults
{
	[JsonPropertyName("vision")]
	public List<VisionResult> Vision { get; set; } = new();

	[JsonPropertyName("match")]
	public List<FallbackResult> Match { get; set; } = new();

	[JsonPropertyName("textual")]
	public List<BaseResult> Textual { get; set; } = new();

	[JsonPropertyName("bestGuess")]
	public TrimmedManga? BestGuess { get; set; }

	[JsonIgnore]
	public bool Success => Vision.Count > 0 || Match.Count > 0 || Textual.Count > 0;

	[JsonIgnore]
	public IEnumerable<BaseResult> All => Match.Concat<BaseResult>(Vision).Concat(Textual);
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

public class BaseResult
{
	[JsonPropertyName("score")]
	public double Score { get; set; }

	[JsonPropertyName("exactMatch")]
	public bool ExactMatch { get; set; }

	[JsonPropertyName("source")]
	public virtual string Source { get; set; } = string.Empty;

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

	public override string Source { get; set; } = "google vision";
}

public class FallbackResult : BaseResult
{
	[JsonPropertyName("metadata")]
	public MangaMetadata? Metadata { get; set; }

	public override string Source { get; set; } = "cba fallback";
}
