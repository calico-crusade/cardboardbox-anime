using Flurl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardboardBox.Anime.Crunchyroll;

using CardboardBox.Extensions;
using Core;
using Core.Models;
using Http;

public interface ICrunchyrollApiService : IAnimeApiService
{
	Task<CrunchyrollResult?> Fetch(string authToken, int start = 0, int size = 50, string sort = "alphabetical", string locale = "en-US");
	IAsyncEnumerable<Anime> All(string? token);
}

public class CrunchyrollApiService : ICrunchyrollApiService
{
	private readonly IApiService _api;
	private readonly ILogger _logger;
	private readonly IConfiguration _config;

	private ICrunchyrollConfig _crunchyConfig => _config.Bind<CrunchyrollConfig>("Crunchyroll");

	public CrunchyrollApiService(
		IApiService api,
		ILogger<CrunchyrollApiService> logger,
		IConfiguration config)
	{
		_api = api;
		_logger = logger;
		_config = config;
	}

	public Task<CrunchyrollResult?> Fetch(string authToken, int start = 0, int size = 50, string sort = "alphabetical", string locale = "en-US")
	{
		var url = _crunchyConfig.ResourceList
			.SetQueryParam("sort_by", sort)
			.SetQueryParam("start", start)
			.SetQueryParam("n", size)
			.SetQueryParam("locale", locale);

		foreach(var (name, res) in _crunchyConfig.Query)
			url.SetQueryParam(name, res);

		return _api.Get<CrunchyrollResult>(url, c => c.Message(c => 
		{
			c.Headers.Add("Authorization", $"Bearer " + authToken);
		}));
	}

	public IAsyncEnumerable<Anime> All() => All(null);

	public async IAsyncEnumerable<Anime> All(string? token)
	{
		if (string.IsNullOrEmpty(token))
			throw new ArgumentNullException(nameof(token));

		_logger.LogInformation("Starting fetching all Crunchyroll data");
		int start = 0,
			size = 100;
		while(true)
		{
			var items = await Fetch(token, start, size);
			if (items == null)
			{
				_logger.LogWarning($"Resource not found for: {start} +{size}");
				break;
			}

			if (items.Items.Count == 0)
			{
				_logger.LogInformation($"Finished finding all data: {start} +{size}");
				break;
			}

			foreach (var item in Convert(items))
				yield return item;

			start += size;
		}
	}

	public IEnumerable<Anime> Convert(CrunchyrollResult? result)
	{
		if (result == null) yield break;

		foreach (var item in result.Items)
			yield return Convert(item);
	}

	public Anime Convert(CrunchyrollResult.Item item)
	{
		return new Anime
		{
			HashId = $"{item.ChannelId}-{item.Id}-{item.Title}".MD5Hash(),
			AnimeId = item.Id,
			Link = item.Type == "series" ? "https://crunchyroll.com/series/" + item.Id : "https://crunchyroll.com/watch/" + item.Id,
			Title = item.Title,
			Description = item.Description,
			PlatformId = item.ChannelId,
			Type = DetermineType(item.Type),
			Metadata = item.Type == "series" ? ConvertMetadata(item.SeriesMetadata) : ConvertMetadata(item.MovieListingMetadata),
			Images = Convert(item.Images).ToList()
		}.Clean();
	}

	public string DetermineType(string type)
	{
		return type switch
		{
			"series" => "series",
			_ => "movie",
		};
	}

	public Metadata ConvertMetadata(CrunchyrollResult.SeriesMetadata series)
	{
		var meta = HandleMetadata(series);
		meta.Tags = series.TenantCategories;

		for (var i = 0; i < series.SeasonCount; i++)
		{
			meta.Seasons.Add(new Season
			{
				EpisodeCount = series.EpisodeCount,
				Type = "series",
				Order = i + 1,
				Number = i + 1,
				Id = i.ToString()
			});
		}

		meta.Ext = new()
		{
			["episodeCount"] = series.EpisodeCount.ToString(),
			["seasonCount"] = series.SeasonCount.ToString(),
			["simulcast"] = series.IsSimulcast.ToString(),
			["availabilityNotes"] = series.AvailabilityNotes,
			["extendedDescription"] = series.ExtendedDescription,
			["isSimulcast"] = series.IsSimulcast.ToString(),
			["seriesLaunchYear"] = series.SeriesLaunchYear.ToString(),
		};

		return meta;
	}

	public Metadata ConvertMetadata(CrunchyrollResult.MovieListingMetadata movie)
	{
		var meta = HandleMetadata(movie);

		meta.Seasons.Add(new Season
		{
			EpisodeCount = 1,
			Number = 1,
			Order = 1,
			Type = "movie",
			Id = "1"
		});

		meta.Ext = new()
		{
			["firstMovieId"] = movie.FirstMovieId,
			["durationMs"] = movie.DurationMs.ToString(),
			["releaseYear"] = movie.MovieReleaseYear.ToString(),
			["premiumOnly"] = movie.IsPremiumOnly.ToString(),
			["availableOffline"] = movie.AvailableOffline.ToString()
		};

		return meta;
	}

	public Metadata HandleMetadata(CrunchyrollResult.Metadata data)
	{
		var meta = new Metadata
		{
			Mature = data.IsMature,
			Ratings = data.MaturityRatings
		};

		if (!data.IsDubbed && !data.IsSubbed)
			meta.LanguageTypes.Add("Unknown");

		if (data.IsDubbed)
		{
			meta.LanguageTypes.Add("Dubbed");
			//We're just assuming that "isDubbed" means it's English.
			//This isn't always the case, but we have no better way to check :/
			meta.Languages.Add("English");
		}

		if (data.IsSubbed)
		{
			meta.LanguageTypes.Add("Subbed");
			meta.Languages.Add("Japanese");
		}

		return meta;
	}

	public IEnumerable<Image> Convert(CrunchyrollResult.Images images)
	{
		foreach (var imageList in images.PosterTall)
			foreach (var image in imageList)
				yield return Convert(image, "poster");


		foreach (var imageList in images.PosterWide)
			foreach (var image in imageList)
				yield return Convert(image, "wallpaper");
	}

	public Image Convert(CrunchyrollResult.Image image, string type)
	{
		return new Image
		{
			Width = image.Width,
			Height = image.Height,
			PlatformId = image.Type,
			Type = type,
			Source = image.Source
		};
	}
}
