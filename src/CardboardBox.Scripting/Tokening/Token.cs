namespace CardboardBox.Scripting.Tokening;

/// <summary>
/// Represents a parsed token
/// </summary>
/// <param name="Content">The content of the token</param>
/// <param name="StartIndex">The start index of this specific token instance</param>
/// <param name="Length">The lenght of the full token</param>
/// <param name="FullToken">The full token as found within the content</param>
public record class Token(
    string Content,
    int StartIndex,
    int Length,
    string FullToken);
