using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class IndexSeason
    {
        [JsonPropertyName("contentId")]
        public string? ContentId { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string> Title { get; set; } = new();

        [JsonPropertyName("episodes")]
        public List<Episode> Episodes { get; set; } = new();

        [JsonPropertyName("availability")]
        public Dictionary<string, Dictionary<string, Dictionary<string, DateTimeSpan>>> Availability { get; set; } = new();
    }
}
