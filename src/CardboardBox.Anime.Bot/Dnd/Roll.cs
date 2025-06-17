using System.Diagnostics.CodeAnalysis;

namespace CardboardBox.Anime.Bot.Dnd;

using NumberGenerator;

/// <summary>
/// Represents a collection of dice that can be rolled together
/// </summary>
public class Roll : List<Dice>, IParsable<Roll>, IRollable
{
    /// <summary>
    /// Gets the damage type that is likely to result in the most amount of damage
    /// </summary>
    public DamageType Type => this
        .GroupBy(t => t.Type)
        .MaxBy(t => t.Sum(t => t.Max))?
        .Key ?? DamageType.Unknown;

    /// <inheritdoc />
    public int Min => this.Sum(t => t.Min);

    /// <inheritdoc />
    public int Max => this.Sum(t => t.Max);

    /// <summary>
    /// The average dice roll value
    /// </summary>
    public double Average => this.Sum(t => t.Average);

    /// <summary>
    /// Attempts to combine all of the dice into as few dice as possible.
    /// </summary>
    /// <remarks>
    /// This can result in different potential averages than if you were to perform the rolls individually.
    /// Only use this if you're aware of the effects and prefer fewer <see cref="Dice"/> in the roll
    /// </remarks>
    public void Condense()
    {
        var combinations = this.GroupBy(t => $"{t.Type}+{t.Sides}").ToArray();
        Clear();
        foreach (var combination in combinations)
        {
            int? sides = null;
            DamageType? type = null;
            int count = 0;
            int modifier = 0;

            foreach(var dice in combination)
            {
                sides ??= dice.Sides;
                type ??= dice.Type;
                count += dice.Count;
                modifier += dice.Modifier;
            }

            if (sides is null) continue;

            type ??= DamageType.Unknown;
            var output = new Dice(sides.Value, count, type.Value, modifier);
            Add(output);
        }
    }

    /// <summary>
    /// Rolls all of the dice and returns the results
    /// </summary>
    /// <param name="type">The type of roll being performed</param>
    /// <param name="generator">The number generator to use</param>
    /// <returns>The roll results</returns>
    public RollResult Results(RollType type = RollType.Regular, INumberGenerator? generator = null)
    {
        var rolls = this
            .Select(dice => dice.Roll(type, generator))
            .ToArray();
        return new(rolls);
    }

    #region Overrides / Operator Methods
    /// <summary>
    /// Returns a string representation of the roll
    /// </summary>
    /// <returns>The string representation of the roll</returns>
    public override string ToString()
    {
        return FormatRoll(this);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Roll"/> instance to a string representation of the dice.
    /// </summary>
    /// <param name="dice">The dice to format</param>
    public static implicit operator string(Roll dice)
    {
        return FormatRoll(dice);
    }

    /// <summary>
    /// Implicitly converts a string representation of dice into a <see cref="Roll"/> instance.
    /// </summary>
    /// <param name="input">The dice to parse</param>
    public static implicit operator Roll(string input)
    {
        return Parse(input);
    }

    /// <summary>
    /// Add two dice together to get a roll
    /// </summary>
    /// <param name="left">The roll to add to</param>
    /// <param name="right">The dice to add</param>
    /// <returns>The roll</returns>
    public static Roll operator +(Roll left, Dice right)
    {
        return [.. left, right];
    }

    /// <summary>
    /// Add two dice together to get a roll
    /// </summary>
    /// <param name="left">The dice to add</param>
    /// <param name="right">The roll to add to</param>
    /// <returns>The roll</returns>
    public static Roll operator +(Dice left, Roll right)
    {
        return [left, .. right];
    }

    /// <summary>
    /// Adds two rolls together to create a new roll that contains all of the dice from both rolls.
    /// </summary>
    /// <param name="left">The roll to union</param>
    /// <param name="right">The roll to union</param>
    /// <returns>The rolls</returns>
    public static Roll operator +(Roll left, Roll right)
    {
        return [..left, .. right];
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Formats the roll as a string representation
    /// </summary>
    /// <param name="roll">The roll to format</param>
    /// <returns>The string representation of the roll</returns>
    public static string FormatRoll(Roll roll)
    {
        return string.Join(" + ", roll.Select(t => t.ToString()));
    }

    /// <inheritdoc />
    public static Roll Parse(string input, IFormatProvider? provider = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace.", nameof(input));
        //ArgumentException.ThrowIfNullOrWhiteSpace(input, nameof(input));
        var rolls = Dice.ParseAll(input);
        return [.. rolls];
    }

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out Roll)" />
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out Roll result)
    {
        return TryParse(s, null, out result);
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Roll result)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            result = null;
            return false;
        }

        var rolls = Dice.ParseAll(s).ToArray();
        if (rolls.Length == 0)
        {
            result = null;
            return false;
        }

        result = [..rolls];
        return true;
    }
    #endregion
}
