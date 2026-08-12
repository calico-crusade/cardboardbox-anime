namespace CardboardBox.LightNovel.Core.Sources.DragonholicSource;

public class DragonholicRenderedText
{
    [JsonPropertyName("rendered")]
    public string Rendered { get; set; } = string.Empty;
}

public class DragonholicSeries
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public DragonholicRenderedText Title { get; set; } = new();

    [JsonPropertyName("content")]
    public DragonholicRenderedText Content { get; set; } = new();

    [JsonPropertyName("_embedded")]
    public DragonholicEmbeddedData? Embedded { get; set; }
}

public class DragonholicChapter
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("parent")]
    public long Parent { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public DragonholicRenderedText Title { get; set; } = new();

    [JsonPropertyName("content")]
    public DragonholicRenderedText Content { get; set; } = new();
}

public class DragonholicChapterCatalog
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("chapters")]
    public DragonholicChapterItem[] Chapters { get; set; } = [];

    [JsonPropertyName("totalChapters")]
    public int TotalChapters { get; set; }

    [JsonPropertyName("unlockedChapterIds")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long[] UnlockedChapterIds { get; set; } = [];

    [JsonPropertyName("hasMore")]
    public bool HasMore { get; set; }
}

public class DragonholicChapterItem
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("heading")]
    public string Heading { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("chapter_order")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal ChapterOrder { get; set; }

    [JsonPropertyName("is_premium")]
    public bool IsPremium { get; set; }

    [JsonPropertyName("volume_id")]
    public string? VolumeId { get; set; }

    [JsonPropertyName("volume_name")]
    public string? VolumeName { get; set; }

    [JsonPropertyName("volume_slug")]
    public string? VolumeSlug { get; set; }

    [JsonPropertyName("volume_order")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? VolumeOrder { get; set; }
}

public class DragonholicEmbeddedData
{
    [JsonPropertyName("wp:featuredmedia")]
    public DragonholicMedia[] FeaturedMedia { get; set; } = [];

    [JsonPropertyName("wp:term")]
    public DragonholicTerm[][] Terms { get; set; } = [];
}

public class DragonholicMedia
{
    [JsonPropertyName("source_url")]
    public string SourceUrl { get; set; } = string.Empty;
}

public class DragonholicTerm
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("taxonomy")]
    public string Taxonomy { get; set; } = string.Empty;
}
