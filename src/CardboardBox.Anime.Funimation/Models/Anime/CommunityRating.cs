using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class CommunityRating
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }
    }
}
