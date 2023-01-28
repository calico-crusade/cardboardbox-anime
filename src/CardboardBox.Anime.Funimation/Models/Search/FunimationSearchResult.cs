using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class FunimationSearchResults
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("items")]
    public Items Items { get; set; } = new();

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}
