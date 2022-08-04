using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Funimation
{
    public class DateTimeSpan
    {
        [JsonPropertyName("start")]
        public DateTime Start { get; set; }

        [JsonPropertyName("end")]
        public DateTime End { get; set; }
    }
}
