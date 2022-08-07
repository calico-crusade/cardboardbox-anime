using CardboardBox.Http;
using Flurl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardboardBox.Anime.Vrv
{
	using Core;
	using Core.Models;

	public interface IVrvApiService : IAnimeApiService
	{
		Task<VrvResourceResult?> Fetch(string query, string sort = "alphabetical", int n = 100, Dictionary<string, string>? pars = null);
		IAsyncEnumerable<Anime> All(VrvLoadRequest? request);
	}

	public class VrvApiService : IVrvApiService
	{
		private readonly IApiService _api;
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		private IVrvConfig _vrvConfig => _config.Bind<VrvConfig>("Vrv");

		public VrvApiService(
			IApiService api, 
			ILogger<VrvApiService> logger, 
			IConfiguration config)
		{
			_api = api;
			_logger = logger;
			_config = config;
		}

		public Task<VrvResourceResult?> Fetch(string query, string sort = "alphabetical", int n = 100, Dictionary<string, string>? pars = null)
		{
			var url = _vrvConfig.ResourceList
				.SetQueryParam("q", query)
				.SetQueryParam("sort_by", sort)
				.SetQueryParam("n", n);

			foreach (var (name, res) in _vrvConfig.Query)
				url.SetQueryParam(name, res);

			if (pars != null)
				foreach (var (name, res) in pars)
					url.SetQueryParam(name, res);

			return _api.Get<VrvResourceResult>(url.ToString());
		}

		public IAsyncEnumerable<Anime> All() => All(null);

		public async IAsyncEnumerable<Anime> All(VrvLoadRequest? request)
		{
			_logger.LogInformation("Starting fetching all VRV data");
			var ops = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
			var pars = request?.ToDictionary();
			foreach(var op in ops)
			{
				var resources = await Fetch(op.ToString(), pars: pars);
				if (resources == null)
				{
					_logger.LogWarning("Resource not found for: " + op);
					continue;
				}

				_logger.LogInformation($"{resources.Total} found for: {op}");

				foreach (var anime in Convert(resources))
					yield return anime;
			}

			_logger.LogInformation("Finsihed fetching all VRV data");
		}

		public IEnumerable<Anime> Convert(VrvResourceResult? results)
		{
			if (results == null) yield break;

			foreach (var result in results.Items)
				yield return ConvertItem(result);
		}

		public Anime ConvertItem(VrvResourceResult.Item item)
		{
			return new Anime
			{
				HashId = $"{item.ChannelId}-{item.Id}-{item.Title}".MD5Hash(),
				AnimeId = item.Id,
				Link = item.Type == "series" ? "https://vrv.co/series/" + item.Id : "https://vrv.co/watch/" + item.Id,
				Title = item.Title,
				Description = item.Description,
				PlatformId = item.ChannelId,
				Type = DeteremineType(item.Type),
				Metadata = item.Type == "series" ? ConvertMetadata(item.SeriesMetadata) : ConvertMetadata(item.MovieListingMetadata),
				Images = ConvertImages(item.Images).ToList()
			};
		}

		public string DeteremineType(string type)
		{
			switch(type)
			{
				case "series": return "series";
				default: return "movie";
			}
		}

		public IEnumerable<Image> ConvertImages(VrvResourceResult.Images images)
		{
			foreach (var imageList in images.PosterTall)
				foreach (var image in imageList)
					yield return ConvertImage(image, "poster");


			foreach (var imageList in images.PosterWide)
				foreach (var image in imageList)
					yield return ConvertImage(image, "wallpaper");


			foreach (var imageList in images.Banner)
				foreach (var image in imageList)
					yield return ConvertImage(image, "banner");
		}

		public Image ConvertImage(VrvResourceResult.ImageResult image, string type)
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

		public Metadata ConvertMetadata(VrvResourceResult.SeriesMetadata series)
		{
			var meta = HandleMetadata(series);
			meta.Tags = series.TenantCategories;

			for(var i = 0; i < series.SeasonCount; i++)
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
				["lastPublicSeasonNumber"] = series.LastPublicSeasonNumber.ToString(),
				["lastPublicEpisodeNumber"] = series.LastPublicEpisodeNumber.ToString()
			};

			return meta;
		}

		public Metadata ConvertMetadata(VrvResourceResult.MovieListingMetadata movie)
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

		public Metadata HandleMetadata(VrvResourceResult.Metadata data)
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
	}
}