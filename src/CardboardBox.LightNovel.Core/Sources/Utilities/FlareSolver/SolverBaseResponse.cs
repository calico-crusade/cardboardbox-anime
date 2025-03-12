namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public abstract class SolverBaseResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("startTimestamp")]
    internal long StartTimestampMilliseconds { get; set; }

    [JsonPropertyName("endTimestamp")]
    internal long EndTimestampMilliseconds { get; set; }

    [JsonIgnore]
    public DateTime StartTimestamp => DateTimeOffset.FromUnixTimeMilliseconds(StartTimestampMilliseconds).DateTime;

    [JsonIgnore]
    public DateTime EndTimestamp => DateTimeOffset.FromUnixTimeMilliseconds(EndTimestampMilliseconds).DateTime;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
