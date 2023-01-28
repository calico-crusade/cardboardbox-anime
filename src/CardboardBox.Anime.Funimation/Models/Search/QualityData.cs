using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class QualityData
{
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}
