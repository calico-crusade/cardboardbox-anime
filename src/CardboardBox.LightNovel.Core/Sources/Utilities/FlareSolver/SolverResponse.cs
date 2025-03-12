namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverResponse : SolverBaseResponse
{
    [JsonPropertyName("solution")]
    public SolverSolution Solution { get; set; } = new();
}