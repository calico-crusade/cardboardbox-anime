using CardboardBox.Anime.Core;

namespace CardboardBox.LightNovel.Core.Sources.ZirusSource;

public class ZirusResponse<T>
{
    [JsonPropertyName("pageProps")]
    public T PageProps { get; set; } = default!;

    [JsonPropertyName("__N_SSG")]
    public bool NSsg { get; set; }
}

public class ZirusSeries : ZirusResponse<ZirusSeries.DataWrapper>
{
    public class DataWrapper
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; } = new();
    }

    public class Data
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("raw")]
        public string Raw { get; set; } = string.Empty;

        [JsonPropertyName("seriesID")]
        public string SeriesId { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("sourceURL")]
        public string SourceUrl { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("authorURL")]
        public string AuthorUrl { get; set; } = string.Empty;

        [JsonPropertyName("translator")]
        public string Translator { get; set; } = string.Empty;

        [JsonPropertyName("ongoing")]
        public bool Ongoing { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("volumes")]
        public Volume[] Volumes { get; set; } = Array.Empty<Volume>();
    }

    public class Volume
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("chapters")]
        public ChapterData[] Chapters { get; set; } = Array.Empty<ChapterData>();

        [JsonPropertyName("translated")]
        public int Translated { get; set; }
    }

    public class ChapterData
    {
        [JsonPropertyName("published")]
        public string? Published { get; set; }

        [JsonPropertyName("translator")]
        public string? Translator { get; set; }

        [JsonPropertyName("subsubtitle")]
        public string? Subsubtitle { get; set; }

        [JsonPropertyName("series")]
        public string Series { get; set; } = string.Empty;

        [JsonPropertyName("volume")]
        public int Volume { get; set; }

        [JsonPropertyName("chapter")]
        public string Chapter { get; set; } = string.Empty;

        [JsonPropertyName("prev")]
        public Next? Prev { get; set; }

        [JsonPropertyName("next")]
        public Next? Next { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        [JsonPropertyName("blurb")]
        public string? Blurb { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Title))
                return Title;

            if (!string.IsNullOrEmpty(Subsubtitle))
                return Subsubtitle;

            if (!string.IsNullOrEmpty(Subtitle))
                return Subtitle;

            return $"Chapter {Chapter} (Vol {Volume})";
        }
    }

    public class Next
    {
        [JsonPropertyName("volume")]
        public int Volume { get; set; }

        [JsonPropertyName("chapter")]
        [JsonConverter(typeof(JsonStringContractResolver))]
        public string Chapter { get; set; } = string.Empty;
    }
}

public class ZirusChapter : ZirusResponse<ZirusChapter.DataWrapper>
{
    public class DataWrapper
    {
        [JsonPropertyName("data")]
        public ZirusSeries.ChapterData Data { get; set; } = new();

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}