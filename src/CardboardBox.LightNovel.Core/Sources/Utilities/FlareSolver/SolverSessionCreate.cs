namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverSessionCreate : SolverBaseResponse
{
    [JsonPropertyName("session")]
    public string SessionId { get; set; } = string.Empty;
}
