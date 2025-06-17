namespace CardboardBox.Anime.Bot.Dnd.NumberGenerator;

/// <summary>
/// A service that generates numbers based on <see cref="System.Random"/>
/// </summary>
/// <param name="seed">The seed for the random number generator</param>
public class StandardNumberGenerator(int? seed = null) : INumberGenerator
{
    private Random? _random;

    /// <summary>
    /// The instance of the random number generator used by this service.
    /// </summary>
    public Random Random => _random ??= seed.HasValue ? new Random(seed.Value) : new Random();

    /// <inheritdoc />
    public int Generate(int min, int max)
    {
        return Random.Next(min, max);
    }
}
