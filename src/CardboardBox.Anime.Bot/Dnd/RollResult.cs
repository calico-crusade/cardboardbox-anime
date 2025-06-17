using System.Collections;

namespace CardboardBox.Anime.Bot.Dnd;

/// <summary>
/// The result of a roll
/// </summary>
public class RollResult(DiceResult[] results) : IReadOnlyCollection<DiceResult>
{
    private Dictionary<DamageType, int>? _damageTotals;

    /// <summary>
    /// Gets all of the dice results of a specific type of damage.
    /// </summary>
    /// <param name="type">The damage type</param>
    /// <returns>The dice results</returns>
    public IEnumerable<DiceResult> this[DamageType type] => results.Where(r => r.Dice.Type == type);

    /// <summary>
    /// The total result of all the dice rolls in this result
    /// </summary>
    public int Total => results.Sum(r => r.Total);

    /// <summary>
    /// The total amount of damage done by type
    /// </summary>
    /// <remarks>This is lazily computed</remarks>
    public Dictionary<DamageType, int> TotalsByDamage => _damageTotals ??= results
        .GroupBy(r => r.Dice.Type)
        .ToDictionary(
            g => g.Key,
            g => g.Sum(r => r.Total));

    /// <inheritdoc />
    public int Count => results.Length;

    /// <inheritdoc />
    public IEnumerator<DiceResult> GetEnumerator() => results.AsEnumerable().GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
