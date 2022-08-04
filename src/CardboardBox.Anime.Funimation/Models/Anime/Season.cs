using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class Season
    {
        [JsonPropertyName("episodeCount")]
        public int EpisodeCount { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new();

        [JsonPropertyName("entitledSeason")]
        public bool EntitledSeason { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("venueId")]
        public int VenueId { get; set; }
    }
}
