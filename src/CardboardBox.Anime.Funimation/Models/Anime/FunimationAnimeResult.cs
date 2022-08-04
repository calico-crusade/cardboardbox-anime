using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class FunimationAnimeResult
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("venueId")]
        public int VenueId { get; set; }

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("index")]
        public Index? Index { get; set; }

        //[JsonPropertyName("licensorLogos")]
        //public List<object> LicensorLogos { get; set; }

        [JsonPropertyName("yearOfProduction")]
        public int YearOfProduction { get; set; }

        [JsonPropertyName("copyright")]
        public string? Copyright { get; set; }

        [JsonPropertyName("shortCopyright")]
        public string? ShortCopyright { get; set; }

        [JsonPropertyName("specialsCount")]
        public int SpecialsCount { get; set; }

        [JsonPropertyName("moviesCount")]
        public int MoviesCount { get; set; }

        [JsonPropertyName("ovaCount")]
        public int OvaCount { get; set; }

        [JsonPropertyName("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("shortSynopsis")]
        public Dictionary<string, string> ShortSynopsis { get; set; } = new();

        [JsonPropertyName("mediumSynopsis")]
        public Dictionary<string, string> MediumSynopsis { get; set; } = new();

        [JsonPropertyName("longSynopsis")]
        public Dictionary<string, string> LongSynopsis { get; set; } = new();

        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new();

        [JsonPropertyName("originalLanguage")]
        public string? OriginalLanguage { get; set; }

        [JsonPropertyName("studios")]
        public List<string> Studios { get; set; } = new();

        [JsonPropertyName("communityRatings")]
        public List<CommunityRating> CommunityRatings { get; set; } = new();

        [JsonPropertyName("qualities")]
        public List<QualityData> Qualities { get; set; } = new();

        [JsonPropertyName("ratings")]
        public List<RatingData> Ratings { get; set; } = new();

        [JsonPropertyName("genres")]
        public List<GenreData> Genres { get; set; } = new();

        [JsonPropertyName("images")]
        public List<Image> Images { get; set; } = new();

        [JsonPropertyName("externalShowId")]
        public string? ExternalShowId { get; set; }

        [JsonPropertyName("isCustomImage")]
        public bool IsCustomImage { get; set; }

        [JsonPropertyName("seasons")]
        public List<Season> Seasons { get; set; } = new();

        [JsonPropertyName("audioLanguages")]
        public List<LanguageData> AudioLanguages { get; set; } = new();

        [JsonPropertyName("subtitleLanguages")]
        public List<LanguageData> SubtitleLanguages { get; set; } = new();

        [JsonPropertyName("tagline")]
        public Dictionary<string, string> Tagline { get; set; } = new();
    }
}
