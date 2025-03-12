namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public class SolverCookie(string name, string value)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("value")]
    public string Value { get; set; } = value;

    [JsonPropertyName("domain")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Domain { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("expiry")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long Expires { get; set; } = 0;

    [JsonPropertyName("secure")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Secure { get; set; } = false;

    [JsonPropertyName("httpOnly")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HttpOnly { get; set; } = false;

    [JsonPropertyName("sameSite")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string SameSite { get; set; } = string.Empty;

    [JsonConstructor]
    internal SolverCookie() : this(string.Empty, string.Empty) { }
}
