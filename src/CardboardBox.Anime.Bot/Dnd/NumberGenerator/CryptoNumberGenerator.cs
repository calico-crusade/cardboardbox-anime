using System.Security.Cryptography;

namespace CardboardBox.Anime.Bot.Dnd.NumberGenerator;

/// <summary>
/// A service that generates numbers based on a cryptographically secure random number generator.
/// </summary>
public class CryptoNumberGenerator : INumberGenerator
{
    /// <inheritdoc />
    public int Generate(int min, int max)
    {
        return RandomNumberGenerator.GetInt32(min, max + 1);
    }
}
