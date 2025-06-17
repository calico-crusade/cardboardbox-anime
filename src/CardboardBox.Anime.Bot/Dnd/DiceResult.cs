namespace CardboardBox.Anime.Bot.Dnd;

/// <summary>
/// A result of a <see cref="DndUtils.Dice"/> roll
/// </summary>
/// <param name="Dice">The dice that is being rolled</param>
/// <param name="Total">The result of the dice roll</param>
/// <param name="Rolls">Each of the individual rolls that were made</param>
/// <param name="AlternateTotal">The alternate dice roll that was made (disadvantage / advantage)</param>
/// <param name="AlternateRolls">Each of the individual rolls that were for the alternative dice (disadvantage / advantage)</param>
/// <param name="Type">The type of roll that was made</param>
public record class DiceResult(
    Dice Dice,
    int Total,
    int[] Rolls,
    int AlternateTotal,
    int[] AlternateRolls,
    RollType Type);
