namespace CardboardBox.Manga;

public class IndexRequest
{
    public const string TYPE_PAGES = "pages";
    public const string TYPE_COVER = "cover";

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mangaId")]
    public string MangaId { get; set; } = string.Empty;

    [JsonPropertyName("chapterId")]
    public string? ChapterId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
