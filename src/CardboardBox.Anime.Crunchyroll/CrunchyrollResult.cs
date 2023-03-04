using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Crunchyroll;

public class CrunchyrollResult
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public List<Item> Items { get; set; } = new();

    [JsonPropertyName("__class__")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("__href__")]
    public string Href { get; set; } = string.Empty;

    [JsonPropertyName("__resource_key__")]
    public string ResourceKey { get; set; } = string.Empty;

    [JsonPropertyName("__links__")]
    public LinksData Links { get; set; } = new();

    public class AdBreak
    {
        [JsonPropertyName("offset_ms")] 
        public int OffsetMs { get; set; } 

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class Link
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;
    }

    public class Images
    {
        [JsonPropertyName("poster_tall")]
        public List<List<Image>> PosterTall { get; set; } = new();

        [JsonPropertyName("poster_wide")]
        public List<List<Image>> PosterWide { get; set; } = new();
    }

    public class Image
	{
		[JsonPropertyName("height")]
		public int Height { get; set; } = new();
        [JsonPropertyName("width")]
		public int Width { get; set; } = new();
        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class Item
    {
        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("last_public")] 
        public DateTime LastPublic { get; set; }

        [JsonPropertyName("new")] 
        public bool New { get; set; }

        [JsonPropertyName("images")]
        public Images Images { get; set; } = new();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("series_metadata")]
        public SeriesMetadata SeriesMetadata { get; set; } = new();

        [JsonPropertyName("__class__")]
        public string Class { get; set; } = string.Empty;

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        [JsonPropertyName("__links__")] 
        public LinksData Links { get; set; } = new();

        [JsonPropertyName("channel_id")] 
        public string ChannelId { get; set; } = string.Empty;

        [JsonPropertyName("promo_title")] 
        public string PromoTitle { get; set; } = string.Empty;

        [JsonPropertyName("slug_title")] 
        public string SlugTitle { get; set; } = string.Empty;

        [JsonPropertyName("type")] 
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("id")] 
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("linked_resource_key")] 
        public string LinkedResourceKey { get; set; } = string.Empty;

        [JsonPropertyName("description")] 
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("promo_description")] 
        public string PromoDescription { get; set; } = string.Empty;

        [JsonPropertyName("__href__")] 
        public string Href { get; set; } = string.Empty;

        [JsonPropertyName("new_content")] 
        public bool NewContent { get; set; }

        [JsonPropertyName("movie_listing_metadata")] 
        public MovieListingMetadata MovieListingMetadata { get; set; } = new();
    }

    public class LinksData
    {
        [JsonPropertyName("resource")] 
        public Link Resource { get; set; } = new();

        [JsonPropertyName("resource/channel")] 
        public Link ResourceChannel { get; set; } = new();

        [JsonPropertyName("continuation")] 
        public Link Continuation { get; set; } = new();
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

        [JsonPropertyName("availability_notes")]
        public string AvailabilityNotes { get; set; } = string.Empty;
    }

    public class MovieListingMetadata : Metadata
    {
        [JsonPropertyName("ad_breaks")] 
        public List<AdBreak> AdBreaks { get; set; } = new();

        [JsonPropertyName("available_offline")]
        public bool AvailableOffline { get; set; }

        [JsonPropertyName("duration_ms")] 
        public int DurationMs { get; set; }

        [JsonPropertyName("extended_description")] 
        public string ExtendedDescription { get; set; } = string.Empty;

        [JsonPropertyName("first_movie_id")] 
        public string FirstMovieId { get; set; } = string.Empty;

        [JsonPropertyName("free_available_date")] 
        public DateTime FreeAvailableDate { get; set; }

        [JsonPropertyName("is_premium_only")] 
        public bool IsPremiumOnly { get; set; }

        [JsonPropertyName("movie_release_year")] 
        public int MovieReleaseYear { get; set; }

        [JsonPropertyName("premium_available_date")] 
        public DateTime PremiumAvailableDate { get; set; }
    }

    public class SeriesMetadata : Metadata
    {
        [JsonPropertyName("episode_count")] 
        public int EpisodeCount { get; set; } = new();

        [JsonPropertyName("extended_description")] 
        public string ExtendedDescription { get; set; } = string.Empty;

        [JsonPropertyName("is_simulcast")]
        public bool IsSimulcast { get; set; } = new();

        [JsonPropertyName("season_count")]
        public int SeasonCount { get; set; } = new();

        [JsonPropertyName("series_launch_year")]
        public int SeriesLaunchYear { get; set; } = new();

        [JsonPropertyName("tenant_categories")]
        public List<string> TenantCategories { get; set; } = new();
    }
}
