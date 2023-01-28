using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class GenreData
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("name")]
    public Dictionary<string, string> Name { get; set; } = new();

    [JsonPropertyName("id")]
    public int Id { get; set; }
}
