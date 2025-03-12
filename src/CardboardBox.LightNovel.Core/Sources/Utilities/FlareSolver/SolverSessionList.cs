namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverSessionList : SolverBaseResponse
{
    [JsonPropertyName("sessions")]
    public string[] SessionIds { get; set; } = [];
}
