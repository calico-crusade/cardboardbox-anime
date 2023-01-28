using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class Items
{
    [JsonPropertyName("hits")]
    public List<Hit> Hits { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("facets")]
    public Facets Facets { get; set; } = new();
}
