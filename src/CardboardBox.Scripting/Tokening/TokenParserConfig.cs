namespace CardboardBox.Scripting.Tokening;

/// <summary>
/// Represents the configuration for the token parser
/// </summary>
public class TokenParserConfig
{
    /// <summary>
    /// The character that delimits the start of a token
    /// </summary>
    public string StartToken { get; set; } = "/*";

    /// <summary>
    /// The character that delimits the end of a token
    /// </summary>
    public string EndToken { get; set; } = "*/";

    /// <summary>
    /// The character to use to escpae the <see cref="StartToken"/>
    /// </summary>
    public string EscapeToken { get; set; } = "\\";

    public TokenParserConfig() { }

    public TokenParserConfig(string startToken, string endToken, string escapeToken)
    {
        StartToken = startToken;
        EndToken = endToken;
        EscapeToken = escapeToken;
    }

    public void Deconstruct(out string start, out string end, out string escape)
    {
        start = StartToken;
        end = EndToken;
        escape = EscapeToken;
    }
}
