using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class Index
    {
        [JsonPropertyName("contentId")]
        public string? ContentId { get; set; }

        [JsonPropertyName("title")]
        public Dictionary<string, string> Title { get; set; } = new();

        [JsonPropertyName("seasons")]
        public List<IndexSeason> Seasons { get; set; } = new();

        [JsonPropertyName("availability")]
        public Dictionary<string, Dictionary<string, Dictionary<string, DateTimeSpan>>> Availability { get; set; } = new();

        [JsonPropertyName("publishTerritoryDates")]
        public Dictionary<string, Dictionary<string, DateTimeSpan>> PublishTerritoryDates { get; set; } = new();
    }
}
