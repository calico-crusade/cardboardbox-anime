using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class Episode
    {
        [JsonPropertyName("contentId")]
        public string? ContentId { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string> Title { get; set; } = new();

        [JsonPropertyName("availability")]
        public Dictionary<string, Dictionary<string, Dictionary<string, DateTimeSpan>>> Availability { get; set; } = new();

        [JsonPropertyName("versions")]
        public List<string> Versions { get; set; } = new();

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }
    }
}
