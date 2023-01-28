using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class Synopsis
{
    [JsonPropertyName("longSynopsis")]
    public string? LongSynopsis { get; set; }

    [JsonPropertyName("shortSynopsis")]
    public string? ShortSynopsis { get; set; }

    [JsonPropertyName("mediumSynopsis")]
    public string? MediumSynopsis { get; set; }
}