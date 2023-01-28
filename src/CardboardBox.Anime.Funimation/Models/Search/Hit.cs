using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation;

public class Hit
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("titleExt")]
    public Dictionary<string, string> TitleExt { get; set; } = new();

    [JsonPropertyName("synopsis")]
    public Synopsis Synopsis { get; set; } = new();

    [JsonPropertyName("synopsisExt")]
    public Dictionary<string, Synopsis> SynopsisExt { get; set; } = new();

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = new();

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    [JsonPropertyName("studios")]
    public List<string> Studios { get; set; } = new();

    [JsonPropertyName("showUrl")]
    public string? ShowUrl { get; set; }

    [JsonPropertyName("showSlug")]
    public string? ShowSlug { get; set; }

    [JsonPropertyName("maturity")]
    public Dictionary<string, bool> Maturity { get; set; } = new();

    [JsonPropertyName("productionYear")]
    public int ProductionYear { get; set; }

    [JsonPropertyName("releaseYear")]
    public int ReleaseYear { get; set; }

    [JsonPropertyName("quality")]
    public QualityData Quality { get; set; } = new();

    [JsonPropertyName("ratingUsTvMpaa")]
    public string? RatingUsTvMpaa { get; set; }

    [JsonPropertyName("ratingPairs")]
    public List<string> RatingPairs { get; set; } = new();

    [JsonPropertyName("venueId")]
    public int VenueId { get; set; }

    [JsonPropertyName("versions")]
    public List<string> Versions { get; set; } = new();

    [JsonPropertyName("videoTypes")]
    public List<string> VideoTypes { get; set; } = new();

    [JsonPropertyName("images")]
    public Images Images { get; set; } = new();

    [JsonPropertyName("relevancy")]
    public int Relevancy { get; set; }
}
