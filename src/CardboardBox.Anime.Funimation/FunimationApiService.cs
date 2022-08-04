using CardboardBox.Http;
using Flurl;
using Microsoft.Extensions.Logging;
using CMImage = CardboardBox.Anime.Core.Models.Image;

namespace CardboardBox.Anime.Funimation
{
	using Core;
	using Core.Models;

	public interface IFunimationApiService : IAnimeApiService
	{
		Task<FunimationSearchResults?> FetchAll();
		Task<FunimationAnimeResult?> Anime(string showUrl);
		Task<FunimationSearchResults?> Search(int offset, Sort sort, params Filter[] filters);
		Task<FunimationSearchResults?> Search(int offset, params Filter[] filters);
		Task<FunimationSearchResults?> Search(params Filter[] filters);
		Task<FunimationSearchResults?> Search(int offset, int limit, Sort sort, params Filter[] filters);
	}

	public class FunimationApiService : IFunimationApiService
	{
		private const string FUNIMATION_SEARCH_URL = "https://search.prd.funimationsvc.com/v1/search";
		private const string FUNIMATION_ANIME = "https://d33et77evd9bgg.cloudfront.net/data/v2";
		private const int LIMIT_DEFAULT = 25;

		private readonly IApiService _api;
		private readonly ILogger _logger;

		public FunimationApiService(
			IApiService api,
			ILogger<FunimationApiService> logger)
		{
			_api = api;
			_logger = logger;
		}

		public Task<FunimationAnimeResult?> Anime(string showUrl)
		{
			var url = $"{FUNIMATION_ANIME}/{showUrl.Trim('/')}.json";
			return _api.Get<FunimationAnimeResult>(url, c =>
			{
				c.Headers.Add("Origin", "https://www.funimation.com");
				c.Headers.Add("Referer", "https://www.funimation.com");
				c.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");
			});
		}

		public Task<FunimationSearchResults?> Search(int offset, Sort sort, params Filter[] filters) => Search(offset, LIMIT_DEFAULT, sort, filters);

		public Task<FunimationSearchResults?> Search(int offset, params Filter[] filters) => Search(offset, LIMIT_DEFAULT, Sort.NameAsc, filters);

		public Task<FunimationSearchResults?> Search(params Filter[] filters) => Search(0, LIMIT_DEFAULT, Sort.NameAsc, filters);

		public Task<FunimationSearchResults?> Search(int offset, int limit, Sort sort, params Filter[] filters)
		{
			var uri = FUNIMATION_SEARCH_URL.SetQueryParams(new
			{
				index = "catalog-shows",
				region = "US",
				limit,
				offset,
				sort = DetermineSort(sort),
				f = GenerateFilters(filters),
				lang = Languages(filters)
			}).ToString();

			return _api.CacheGet<FunimationSearchResults>(uri, c =>
			{
				c.Headers.Add("Origin", "https://www.funimation.com");
				c.Headers.Add("Referer", "https://www.funimation.com");
				c.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:94.0) Gecko/20100101 Firefox/94.0");
			});
		}

		public async Task<FunimationSearchResults?> FetchAll()
		{
			return await Search(0, 1000, Sort.NameAsc);
		}

		public async IAsyncEnumerable<Anime> All()
		{
			var data = await FetchAll();
			if (data?.Items?.Hits == null || data.Items.Hits.Count == 0)
			{
				_logger.LogError("Funimation return results was empty / null");
				yield break;
			}

			foreach (var item in data.Items.Hits)
				yield return ConvertItem(item);
		}

		public Anime ConvertItem(Hit item)
		{
			return new Anime
			{
				HashId = $"funimation-{item.Id}-{item.Title}".MD5Hash(),
				AnimeId = item.Id ?? "",
				Link = "https://funimation.com" + item.ShowUrl,
				Title = item.Title ?? "",
				Description = item.Synopsis.LongSynopsis ?? "",
				PlatformId = "funimation",
				Type = DeteremineType(item.Type),
				Metadata = ConvertMetadata(item),
				Images = ConvertImage(item.Images).ToList()
			};
		}

		public Metadata ConvertMetadata(Hit item)
		{
			var meta = new Metadata
			{
				Languages = item.Languages,
				Tags = item.Genres,
				Ratings = item.RatingPairs,
				Mature = item.Maturity.ContainsKey("usa") && item.Maturity["usa"],
			};

			if (!item.Languages.Contains("English") && !item.Languages.Contains("Japanese"))
				meta.LanguageTypes.Add("Unknown");

			if (item.Languages.Contains("English"))
				meta.LanguageTypes.Add("Dubbed");
			if (item.Languages.Contains("Japanese"))
				meta.LanguageTypes.Add("Subbed");

			meta.Ext = new()
			{
				["studios"] = string.Join(",", item.Studios),
				["slug"] = item.ShowSlug ?? "",
				["productionYear"] = item.ProductionYear.ToString(),
				["releaseYear"] = item.ReleaseYear.ToString(),
				["quality"] = item.Quality.Height.ToString(),
				["ratingUsTvMpaa"] = item.RatingUsTvMpaa ?? "",
				["versions"] = string.Join(",", item.Versions),
				["videoTypes"] = string.Join(",", item.VideoTypes),
				["venueId"] = item.VenueId.ToString()
			};

			return meta;
		}

		public IEnumerable<CMImage> ConvertImage(Images images)
		{
			var props = typeof(Images).GetProperties();
			foreach(var prop in props)
			{
				var value = prop.GetValue(images)?.ToString();

				if (string.IsNullOrEmpty(value)) continue;

				var (width, height, type) = DetermineType(prop.Name);

				yield return new CMImage
				{
					Type = type,
					PlatformId = prop.Name,
					Source = value,
					Width = width,
					Height = height
				};
			}
		}

		public (int? Width, int? Height, string Type) DetermineType(string name)
		{
			return name switch
			{
				"AppleHorizontalBannerShow" => (3840, 2160, "wallpaper"),
				"AppleSquareCover" => (3000, 3000, "other"),
				"BackgroundImageAppletvfiretv" => (1920, 1080, "wallpaper"),
				"BackgroundImageXbox360" => (1280, 720, "wallpaper"),
				"ContinueWatchingDesktop" => (1920, 750, "banner"),
				"ContinueWatchingMobile" => (1080, 1080, "other"),
				"FeaturedSpotlightShowPhone" => (1500, 1332, "wallpaper"),
				"FeaturedSpotlightShowTablet" => (4096, 1500, "banner"),
				"NewShowDetailHero" => (1920, 750, "banner"),
				"NewShowDetailHeroPhone" => (1080, 1080, "other"),
				"ShowBackgroundSite" => (1600, 700, "wallpaper"),
				"ShowDetailBoxArtPhone" => (1080, 1080, "other"),
				"ShowDetailBoxArtTablet" => (1728, 2394, "poster"),
				"ShowDetailBoxArtXbox360" => (384, 384, "other"),
				"ShowDetailHeaderDesktop" => (1920, 750, "banner"),
				"ShowDetailHeaderMobile" => (1500, 1090, "wallpaper"),
				"ShowKeyart" => (2000, 3000, "poster"),
				"ShowMasterKeyArt" => (3000, 1688, "banner"),
				"ShowThumbnail" => (3456, 3456, "other"),
				"ApplePosterCover" => (2000, 3000, "poster"),
				_ => (null, null, "other"),
			};
		}

		public static string DeteremineType(string? type)
		{
			switch(type)
			{
				case "Show": return "series";
				default: return "movie";
			}
		}

		public static string GenerateFilters(Filter[] filters) => string.Join(",", filters.Select(t => $"{t.Key}|{t.Value}"));

		public static string Languages(Filter[] filters) => string.Join(",", filters.Where(t => t.Key == "language").Select(t => t.Value));

		public static string DetermineSort(Sort sort)
		{
			return sort switch
			{
				Sort.DateAsc => "latestAvail|asc",
				Sort.DateDesc => "latestAvail|desc",
				Sort.NameDesc => "title|desc",
				_ => "title|asc",
			};
		}
	}
}