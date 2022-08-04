using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class LanguageData
    {
        [JsonPropertyName("name")]
        public Dictionary<string, string> Name { get; set; } = new();

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }
    }
}
