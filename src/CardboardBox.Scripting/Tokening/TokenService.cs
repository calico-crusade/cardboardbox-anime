namespace CardboardBox.Scripting.Tokening;

public interface ITokenService
{
    /// <summary>
    /// Parses all of the tokens out of the given input
    /// </summary>
    /// <param name="input">The input to parse</param>
    /// <param name="config">The optional configuration settings for the parser</param>
    /// <returns>A collection of all of the parsed tokens</returns>
    /// <exception cref="ArgumentException">Thrown if any of the config is invalids</exception>
    IEnumerable<Token> ParseTokens(string input, TokenParserConfig? config = null);
}

public class TokenService : ITokenService
{
    /// <summary>
    /// Parses all of the tokens out of the given input
    /// </summary>
    /// <param name="input">The input to parse</param>
    /// <param name="config">The optional configuration settings for the parser</param>
    /// <returns>A collection of all of the parsed tokens</returns>
    /// <exception cref="ArgumentException">Thrown if any of the config is invalids</exception>
    public IEnumerable<Token> ParseTokens(string input, TokenParserConfig? config = null)
    {
        config ??= new();

        if (string.IsNullOrEmpty(config.StartToken)) throw new ArgumentException("Start token is not valid", nameof(config));
        if (string.IsNullOrEmpty(config.EndToken)) throw new ArgumentException("End token is not valid", nameof(config));
        if (string.IsNullOrEmpty(config.EscapeToken)) throw new ArgumentException("Escape token is not valid", nameof(config));

        int index = 0;
        while (true)
        {
            var (token, i) = FindNextToken(input, config, index);
            if (token == null) break;

            index = i;
            yield return token;
        }
    }

    /// <summary>
    /// Checks if the given token appears immediately before the given index
    /// </summary>
    /// <param name="input">The input to parse through</param>
    /// <param name="token">The token to check for</param>
    /// <param name="index">The index to check before</param>
    /// <returns>Whether or not the token was present</returns>
    public bool PreviousWas(string input, string token, int index)
    {
        var i = index - token.Length;
        if (i < 0) return false;

        var capture = input.Substring(i, token.Length);
        return capture == token;
    }

    /// <summary>
    /// Gets the index of the token from the current index with a fail safe for out of range arguments
    /// </summary>
    /// <param name="input">The input to parse through</param>
    /// <param name="token">The token to check for</param>
    /// <param name="startIndex">The index to start checking at</param>
    /// <returns>The index of the token or -1 if it wasn't found</returns>
    public int IndexOfStart(string input, string token, int startIndex)
    {
        if (startIndex >= input.Length) return -1;
        return input.IndexOf(token, startIndex);
    }

    /// <summary>
    /// Finds the next token from the input
    /// </summary>
    /// <param name="input">The input to parse through</param>
    /// <param name="config">The parser configuration</param>
    /// <param name="index">The index to start at</param>
    /// <returns>A tuple that contains the token (or null if not found) and the ending index</returns>
    public (Token? token, int index) FindNextToken(string input, TokenParserConfig config, int index)
    {
        var (start, end, escape) = config;

        var ts = input.IndexOf(start, index);
        if (ts == -1) return (null, index);

        if (PreviousWas(input, escape, ts))
        {
            return FindNextToken(input, config, ts + start.Length);
        }

        var te = IndexOfStart(input, end, ts + 1);
        if (te == -1) return (null, index);

        index = te;
        var len = te - ts;
        var token = input.Substring(ts + start.Length, len - start.Length);
        var full = input.Substring(ts, len + end.Length);
        return (new Token(token, ts, len + end.Length, full), index);
    }
}
