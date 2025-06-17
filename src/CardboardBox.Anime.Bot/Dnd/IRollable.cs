namespace CardboardBox.Anime.Bot.Dnd;

/// <summary>
/// Represents something that can be rolled
/// </summary>
public interface IRollable
{
    /// <summary>
    /// The type of damage this dice represents
    /// </summary>
    DamageType Type { get; }

    /// <summary>
    /// The lowest possible value when rolling this dice
    /// </summary>
    int Min { get; }

    /// <summary>
    /// The highest possible value when rolling this dice
    /// </summary>
    int Max { get; }
}
