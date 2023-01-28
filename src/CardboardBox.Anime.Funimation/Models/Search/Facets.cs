using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class Facets
{
    [JsonPropertyName("studio")]
    public List<Facet> Studio { get; set; } = new();

    [JsonPropertyName("videoType")]
    public List<Facet> VideoType { get; set; } = new();

    [JsonPropertyName("genre")]
    public List<Facet> Genre { get; set; } = new();

    [JsonPropertyName("language")]
    public List<Facet> Language { get; set; } = new();

    [JsonPropertyName("productionYear")]
    public List<Facet> ProductionYear { get; set; } = new();

    [JsonPropertyName("type")]
    public List<Facet> Type { get; set; } = new();

    [JsonPropertyName("version")]
    public List<Facet> Version { get; set; } = new();
}
