using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Vrv
{
    public class VrvResourceResult
    {
        [JsonPropertyName("__class__")] 
        public string Class { get; set; } = "";

        [JsonPropertyName("__href__")] 
        public string Href { get; set; } = "";

        [JsonPropertyName("__resource_key__")] 
        public string ResourceKey { get; set; } = "";

        [JsonPropertyName("__links__")] 
        public LinkData Links { get; set; } = new();

        [JsonPropertyName("experiment")] 
        public string Experiment { get; set; } = "";

        [JsonPropertyName("total")] 
        public int Total { get; set; }

        [JsonPropertyName("items")] 
        public List<Item> Items { get; set; } = new();

        public class ImageResult
        {
            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("source")]
            public string Source { get; set; } = "";
        }

        public class Images
        {
            [JsonPropertyName("poster_tall")]
            public List<List<ImageResult>> PosterTall { get; set; } = new();

            [JsonPropertyName("poster_wide")]
            public List<List<ImageResult>> PosterWide { get; set; } = new();

            [JsonPropertyName("banner")]
            public List<List<ImageResult>> Banner { get; set; } = new();
        }

        public class Item
        {
            [JsonPropertyName("__class__")]
            public string Class { get; set; } = "";

            [JsonPropertyName("__href__")]
            public string Href { get; set; } = "";

            [JsonPropertyName("__resource_key__")]
            public string ResourceKey { get; set; } = "";

            [JsonPropertyName("__links__")]
            public LinkData Links { get; set; } = new();

            [JsonPropertyName("id")]
            public string Id { get; set; } = "";

            [JsonPropertyName("external_id")]
            public string ExternalId { get; set; } = "";

            [JsonPropertyName("channel_id")]
            public string ChannelId { get; set; } = "";

            [JsonPropertyName("title")]
            public string Title { get; set; } = "";

            [JsonPropertyName("description")]
            public string Description { get; set; } = "";

            [JsonPropertyName("promo_title")]
            public string PromoTitle { get; set; } = "";

            [JsonPropertyName("promo_description")]
            public string PromoDescription { get; set; } = "";

            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("slug")]
            public string Slug { get; set; } = "";

            [JsonPropertyName("images")]
            public Images Images { get; set; } = new();

            [JsonPropertyName("series_metadata")]
            public SeriesMetadata SeriesMetadata { get; set; } = new();

            [JsonPropertyName("locale")]
            public string Locale { get; set; } = "";

            [JsonPropertyName("search_metadata")]
            public SearchMetadata SearchMetadata { get; set; } = new();

            [JsonPropertyName("last_public")]
            public DateTime LastPublic { get; set; }

            [JsonPropertyName("new")]
            public bool New { get; set; } = false;

            [JsonPropertyName("linked_resource_key")]
            public string LinkedResourceKey { get; set; } = "";

            [JsonPropertyName("movie_listing_metadata")]
            public MovieListingMetadata MovieListingMetadata { get; set; } = new();
        }

        public class LinkData
        {
            [JsonPropertyName("resource")]
            public Resource Resource { get; set; } = new();

            [JsonPropertyName("resource/channel")]
            public Resource ResourceChannel { get; set; } = new();
        }

        public class Metadata
        {
            [JsonPropertyName("is_mature")]
            public bool IsMature { get; set; }

            [JsonPropertyName("mature_blocked")]
            public bool MatureBlocked { get; set; }

            [JsonPropertyName("is_subbed")]
            public bool IsSubbed { get; set; }

            [JsonPropertyName("is_dubbed")]
            public bool IsDubbed { get; set; }

            [JsonPropertyName("maturity_ratings")]
            public List<string> MaturityRatings { get; set; } = new();
        }

        public class MovieListingMetadata : Metadata
        {
            [JsonPropertyName("first_movie_id")]
            public string FirstMovieId { get; set; } = "";

            [JsonPropertyName("duration_ms")]
            public int DurationMs { get; set; }

            [JsonPropertyName("movie_release_year")]
            public int MovieReleaseYear { get; set; }

            [JsonPropertyName("is_premium_only")]
            public bool IsPremiumOnly { get; set; }

            [JsonPropertyName("available_offline")]
            public bool AvailableOffline { get; set; }
        }

        public class Resource
        {
            [JsonPropertyName("href")]
            public string Href { get; set; } = "";
        }

        public class SearchMetadata
        {
            [JsonPropertyName("score")]
            public int Score { get; set; }

            [JsonPropertyName("rank")]
            public int Rank { get; set; }

            [JsonPropertyName("popularity_score")]
            public double PopularityScore { get; set; }
        }

        public class SeriesMetadata : Metadata
        {
            [JsonPropertyName("episode_count")]
            public int EpisodeCount { get; set; }

            [JsonPropertyName("season_count")]
            public int SeasonCount { get; set; }

            [JsonPropertyName("is_simulcast")]
            public bool IsSimulcast { get; set; }

            [JsonPropertyName("last_public_season_number")]
            public int LastPublicSeasonNumber { get; set; }

            [JsonPropertyName("last_public_episode_number")]
            public int LastPublicEpisodeNumber { get; set; }

            [JsonPropertyName("tenant_categories")]
            public List<string> TenantCategories { get; set; } = new();
        }
    }
}
