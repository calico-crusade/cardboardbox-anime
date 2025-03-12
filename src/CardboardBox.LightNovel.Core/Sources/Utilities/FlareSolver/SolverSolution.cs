namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverSolution
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("cookies")]
    public SolverCookie[] Cookies { get; set; } = [];

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; } = string.Empty;

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = [];

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}
