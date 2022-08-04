using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Vrv
{
	public class VrvAnime
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = "";

		[JsonPropertyName("link")]
		public string Link { get; set; } = "";

		[JsonPropertyName("images")]
		public List<Image> Images { get; set; } = new();

		[JsonPropertyName("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("description")]
		public string Description { get; set; } = "";

		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("channelId")]
		public string ChannelId { get; set; } = "";

		[JsonPropertyName("metadata")]
		public MetaData Metadata { get; set; } = new();

		public class Image
		{
			[JsonPropertyName("width")]
			public int Width { get; set; }

			[JsonPropertyName("height")]
			public int Height { get; set; }

			[JsonPropertyName("type")]
			public string Type { get; set; } = "";

			[JsonPropertyName("source")]
			public string Source { get; set; } = "";

			public static implicit operator Image(VrvResourceResult.ImageResult i)
			{
				return new Image
				{
					Width = i.Width,
					Height = i.Height,
					Type = i.Type,
					Source = i.Source,
				};
			}
		}

		public class MetaData
		{
			[JsonPropertyName("mature")]
			public bool IsMature { get; set; }

			[JsonPropertyName("matureBlocked")]
			public bool MatureBlocked { get; set; }

			[JsonPropertyName("subbed")]
			public bool IsSubbed { get; set; }

			[JsonPropertyName("dubbed")]
			public bool IsDubbed { get; set; }

			[JsonPropertyName("ratings")]
			public List<string> MaturityRatings { get; set; } = new();

			[JsonPropertyName("series")]
			public SeriesMetadata? Series { get; set; }

			[JsonPropertyName("movie")]
			public MovieMetadata? Movie { get; set; }

			public static implicit operator MetaData(VrvResourceResult.SeriesMetadata m)
			{
				return new MetaData
				{
					IsMature = m.IsMature,
					MatureBlocked = m.MatureBlocked,
					IsSubbed = m.IsSubbed,
					IsDubbed = m.IsDubbed,
					MaturityRatings = m.MaturityRatings,
					Series = new SeriesMetadata
					{
						EpisodeCount = m.EpisodeCount,
						SeasonCount = m.SeasonCount,
						IsSimulcast = m.IsSimulcast,
						LastPublicEpisodeNumber = m.LastPublicEpisodeNumber,
						LastPublicSeasonNumber = m.LastPublicSeasonNumber,
						TenantCategories = m.TenantCategories
					}
				};
			}

			public static implicit operator MetaData(VrvResourceResult.MovieListingMetadata m)
			{
				return new MetaData
				{
					IsMature = m.IsMature,
					MatureBlocked = m.MatureBlocked,
					IsSubbed = m.IsSubbed,
					IsDubbed = m.IsDubbed,
					MaturityRatings = m.MaturityRatings,
					Movie = new MovieMetadata
					{
						FirstMovieId = m.FirstMovieId,
						DurationMs = m.DurationMs,
						MovieReleaseYear = m.MovieReleaseYear,
						IsPremiumOnly = m.IsPremiumOnly,
						AvailableOffline = m.AvailableOffline
					}
				};
			}
		}

		public class SeriesMetadata
		{
			[JsonPropertyName("episodeCount")]
			public int EpisodeCount { get; set; }

			[JsonPropertyName("seasonCount")]
			public int SeasonCount { get; set; }

			[JsonPropertyName("simulcast")]
			public bool IsSimulcast { get; set; }

			[JsonPropertyName("lastPublicSeasonNumber")]
			public int LastPublicSeasonNumber { get; set; }

			[JsonPropertyName("lastPublicEpisodeNumber")]
			public int LastPublicEpisodeNumber { get; set; }

			[JsonPropertyName("tenantCategories")]
			public List<string> TenantCategories { get; set; } = new();
		}

		public class MovieMetadata
		{
			[JsonPropertyName("firstMovieId")]
			public string FirstMovieId { get; set; } = "";

			[JsonPropertyName("durationMs")]
			public int DurationMs { get; set; }

			[JsonPropertyName("movieReleaseYear")]
			public int MovieReleaseYear { get; set; }

			[JsonPropertyName("premiumOnly")]
			public bool IsPremiumOnly { get; set; }

			[JsonPropertyName("availableOffline")]
			public bool AvailableOffline { get; set; }
		}

		public static implicit operator VrvAnime(VrvResourceResult.Item item)
		{
			var images = new List<Image>()
				.Concat(item.Images.PosterTall.SelectMany(t => t).Select(t => (Image)t))
				.Concat(item.Images.PosterWide.SelectMany(t => t).Select(t => (Image)t))
				.Concat(item.Images.Banner.SelectMany(t => t).Select(t => (Image)t))
				.ToList();

			return new VrvAnime
			{
				Id = item.Id,
				Link = item.Type == "series" ? "https://vrv.co/series/" + item.Id : "https://vrv.co/watch/" + item.Id,
				Images = images,
				Title = item.Title,
				Description = item.Description,
				Type = item.Type,
				ChannelId = item.ChannelId,
				Metadata = item.Type == "series" ? (MetaData)item.SeriesMetadata : (MetaData)item.MovieListingMetadata
			};
		}
	}
}
