using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class RatingData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("mature")]
        public bool Mature { get; set; }

        [JsonPropertyName("ratingSystem")]
        public string? RatingSystem { get; set; }

        [JsonPropertyName("ratingCode")]
        public string? RatingCode { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }
    }
}
