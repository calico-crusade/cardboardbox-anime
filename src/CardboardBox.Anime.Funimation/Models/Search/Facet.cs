using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class Facet
    {
        [JsonPropertyName("term")]
        public string Term { get; set; } = "";

        [JsonPropertyName("count")]
        public int Count { get; set; }       
    }
}
