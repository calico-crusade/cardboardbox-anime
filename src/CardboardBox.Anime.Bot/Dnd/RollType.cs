namespace CardboardBox.Anime.Bot.Dnd;

/// <summary>
/// The type of roll that is being performed
/// </summary>
public enum RollType
{
    /// <summary>
    /// A straight roll of the dice
    /// </summary>
    Regular = 0,
    /// <summary>
    /// A roll with advantage, meaning the highest of two rolls is taken
    /// </summary>
    Advantage = 1,
    /// <summary>
    /// A roll with disadvantage, meaning the lowest of two rolls is taken
    /// </summary>
    Disadvantage = 2,
}
