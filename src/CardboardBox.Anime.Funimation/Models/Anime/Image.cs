using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class Image
    {
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }
}
