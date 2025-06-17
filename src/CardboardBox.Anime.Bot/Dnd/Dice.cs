using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CardboardBox.Anime.Bot.Dnd;

using NumberGenerator;

/// <summary>
/// Represents a dice that can be rolled
/// </summary>
/// <param name="sides">The number of sides on the dice</param>
/// <param name="count">The number of dice to be rolled</param>
/// <param name="type">The type of damage this dice is for</param>
/// <param name="modifier">The modifier for the dice</param>
public readonly partial struct Dice(
    int sides,
    int count = 1,
    DamageType type = DamageType.Unknown,
    int modifier = 0) : IParsable<Dice>, IRollable
{
    /// <summary>
    /// The default number generator used for rolling dice.
    /// </summary>
    public static INumberGenerator DefaultNumberGenerator { get; set; } = new StandardNumberGenerator();

    #region Default Dice
    /// <summary>
    /// A 100 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D100 { get; } = new(100);

    /// <summary>
    /// A 20 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D20 { get; } = new(20);

    /// <summary>
    /// A 12 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D12 { get; } = new(12);

    /// <summary>
    /// A 10 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D10 { get; } = new(10);

    /// <summary>
    /// A 8 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D8 { get; } = new(8);

    /// <summary>
    /// A 6 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D6 { get; } = new(6);

    /// <summary>
    /// A 4 sided dice, with no modifier and no damage type.
    /// </summary>
    public static Dice D4 { get; } = new(4);
    #endregion

    #region Properties
    /// <summary>
    /// The number of sides on the dice
    /// </summary>
    public readonly int Sides { get; } = sides;

    /// <summary>
    /// The number of dice to be rolled
    /// </summary>
    public readonly int Count { get; } = count;

    /// <summary>
    /// The type of damage this dice is for
    /// </summary>
    public readonly DamageType Type { get; } = type;

    /// <summary>
    /// The modifier for the dice
    /// </summary>
    public readonly int Modifier { get; } = modifier;

    /// <inheritdoc />
    public readonly int Min => Count + Modifier;

    /// <inheritdoc />
    public readonly int Max => (Count * Sides) + Modifier;

    /// <summary>
    /// The average dice roll value, including the modifier.
    /// </summary>
    public readonly double Average => (Min + Max) / 2.0;
    #endregion

    #region Methods
    /// <summary>
    /// Rolls the given number of dice with the specified number of sides using the provided generator.
    /// </summary>
    /// <param name="count">The number of dice</param>
    /// <param name="sides">The number of sides on the dice</param>
    /// <param name="generator">The number generator to use</param>
    /// <returns>All of the dice roll results</returns>
    public static int[] Roll(int count, int sides, INumberGenerator generator)
    {
        return Enumerable
            .Repeat(1, count)
            .Select(_ => generator.Generate(sides))
            .ToArray();
    }

    /// <summary>
    /// Rolls the dice and applies the modifier to the result
    /// </summary>
    /// <param name="type">The type of roll to make</param>
    /// <param name="generator">The optional number generator to use</param>
    /// <returns>The value that was rolled</returns>
    public DiceResult Roll(RollType type = RollType.Regular, INumberGenerator? generator = null)
    {
        var gen = generator ?? DefaultNumberGenerator;
        if (type == RollType.Regular)
        {
            var regularRoll = Roll(Count, Sides, gen);
            var regularTotal = regularRoll.Sum() + Modifier;
            return new(this, regularTotal, regularRoll, 0, [], type);
        }

        var firstRoll = Roll(Count, Sides, gen);
        var firstTotal = firstRoll.Sum() + Modifier;

        var secondRoll = Roll(Count, Sides, gen);
        var secondTotal = secondRoll.Sum() + Modifier;

        var isFirst = type == RollType.Advantage 
            ? firstTotal >= secondTotal
            : firstTotal <= secondTotal;

        return new(this,
            isFirst ? firstTotal : secondTotal,
            isFirst ? firstRoll : secondRoll,
            isFirst ? secondTotal : firstTotal,
            isFirst ? secondRoll : firstRoll,
            type);
    }
    #endregion

    #region Override / Operator Methods
    /// <summary>
    /// Returns a string representation of the dice in the format "XdY+Z(type)"
    /// </summary>
    /// <returns>The string representation of the dice</returns>
    public override string ToString()
    {
        return FormatDice(this);
    }

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Dice dice &&
               Sides == dice.Sides &&
               Count == dice.Count &&
               Type == dice.Type &&
               Modifier == dice.Modifier;
    }

    /// <summary>
    /// Checks if two <see cref="Dice"/> instances are equal based on their properties.
    /// </summary>
    /// <param name="left">The item to check</param>
    /// <param name="right">The item to check against</param>
    /// <returns>Whether or not the two items are equal</returns>
    public static bool operator ==(Dice left, Dice right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Checks if two <see cref="Dice"/> instances are not equal based on their properties.
    /// </summary>
    /// <param name="left">The item to check</param>
    /// <param name="right">The item to check against</param>
    /// <returns>Whether or not the two items are not equal</returns>
    public static bool operator !=(Dice left, Dice right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Sides, Count, Type, Modifier);
    }

    /// <summary>
    /// Implicitly converts a <see cref="Dice"/> instance to a string representation of the dice.
    /// </summary>
    /// <param name="dice">The dice to format</param>
    public static implicit operator string(Dice dice)
    {
        return FormatDice(dice);
    }

    /// <summary>
    /// Implicitly converts a string representation of dice into a <see cref="Dice"/> instance.
    /// </summary>
    /// <param name="input">The dice to parse</param>
    public static implicit operator Dice(string input)
    {
        return Parse(input);
    }
    
    /// <summary>
    /// Add two dice together to get a roll
    /// </summary>
    /// <param name="left">The first dice</param>
    /// <param name="right">The second dice</param>
    /// <returns>The roll</returns>
    public static Roll operator +(Dice left, Dice right)
    {
        return [left, right];
    }

    /// <summary>
    /// Applies the modifier to the dice
    /// </summary>
    /// <param name="dice">The dice to modify</param>
    /// <param name="modifier">The modifier to add to the base modifier</param>
    /// <returns>The modified dice</returns>
    public static Dice operator +(Dice dice, int modifier)
    {
        return new Dice(dice.Sides, dice.Count, dice.Type, dice.Modifier + modifier);
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Format the given dice into a string representation
    /// </summary>
    /// <param name="dice">The dice to format</param>
    /// <returns>The formatted dice</returns>
    public static string FormatDice(Dice dice)
    {
        var modifier = dice.Modifier switch
        {
            0 => string.Empty,
            < 0 => $"{dice.Modifier}",
            _ => $"+{dice.Modifier}"
        };
        var count = dice.Count > 1 ? $"{dice.Count}" : string.Empty;
        var type = dice.Type != DamageType.Unknown ? $"({dice.Type.ToString().ToLowerInvariant()})" : string.Empty;
        return $"{count}D{dice.Sides}{modifier} {type}".Trim();
    }

    /// <summary>
    /// Attempts to parse the given string input into a <see cref="Dice"/> instance.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>The dice value or null if the input is invalid</returns>
    internal static Dice? ParseInternal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim().ToLowerInvariant();
        var regex = DiceParseRegexExclusive();
        var match = regex.Match(input);
        if (!match.Success)
            return null;

        var count = match.Groups[1].Value;
        var sides = match.Groups[2].Value;
        var modifier = match.Groups[3].Value;
        var type = match.Groups[4].Value;

        if (!int.TryParse(sides, out var sidesValue) || sidesValue < 1)
            return null;

        if (!int.TryParse(count, out var countValue) || countValue < 1)
            countValue = 1;

        if (!int.TryParse(modifier.Trim('+'), out var modifierValue))
            modifierValue = 0;

        var typeValue = ParseDamageType(type);
        return new Dice(sidesValue, countValue, typeValue, modifierValue);
    }

    /// <inheritdoc />
    public static Dice Parse(string input, IFormatProvider? provider = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace.", nameof(input));
        return ParseInternal(input)
            ?? throw new FormatException($"The input '{input}' could not be parsed as a valid dice format.");
    }

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out Dice)" />
    public static bool TryParse([NotNullWhen(true)] string? input, [MaybeNullWhen(false)] out Dice result)
    {
        return TryParse(input, null, out result);
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? input, IFormatProvider? provider, [MaybeNullWhen(false)] out Dice result)
    {
        var parsed = ParseInternal(input);
        if (parsed is null)
        {
            result = default;
            return false;
        }

        result = parsed.Value;
        return true;
    }

    /// <summary>
    /// Parses all dice expressions from the given input string.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>All of the dice listed in the input</returns>
    public static IEnumerable<Dice> ParseAll(string input)
    {
        var regex = DiceParseRegexInclusive();
        var matches = regex.Matches(input.ToLowerInvariant().Trim());
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (!match.Success) continue;

            var parsed = ParseInternal(match.Value);
            if (parsed is not null)
                yield return parsed.Value;
        }
    }

    /// <summary>
    /// Attempts to parse the damage type from the given string input.
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>The damage type</returns>
    public static DamageType ParseDamageType(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return DamageType.Unknown;

        input = input
            .ToLowerInvariant()
            .Replace(")", string.Empty)
            .Replace("(", string.Empty)
            .Trim();
        if (Enum.TryParse<DamageType>(input, true, out var type))
            return type;

        if (input.StartsWith('a')) return DamageType.Acid;
        if (input.StartsWith('b')) return DamageType.Bludgeoning;
        if (input.StartsWith('c')) return DamageType.Cold;
        if (input.StartsWith('l')) return DamageType.Lightning;
        if (input.StartsWith('n')) return DamageType.Necrotic;
        if (input.StartsWith('r')) return DamageType.Radiant;
        if (input.StartsWith('s')) return DamageType.Slashing;

        var aliases = new Dictionary<string, DamageType>
        {
            ["fi"] = DamageType.Fire,
            ["fir"] = DamageType.Fire,
            ["fo"] = DamageType.Force,
            ["for"] = DamageType.Force,
            ["pr"] = DamageType.Piercing,
            ["prc"] = DamageType.Piercing,
            ["po"] = DamageType.Poison,
            ["psn"] = DamageType.Poison,
            ["poi"] = DamageType.Poison,
            ["py"] = DamageType.Psychic,
            ["psy"] = DamageType.Psychic,
        };

        if (aliases.TryGetValue(input, out type))
            return type;

        return DamageType.Unknown;
    }
    #endregion

    #region Regex Stuff

    //[StringSyntax("Regex")]
    private const string DICE_REGEX = @"(\d+)?d(\d+)([+-]\d+)?\s*(\(\s*[a-z]{1,}\s*\))?";

    //[GeneratedRegex(DICE_REGEX)]
    //private static partial Regex DiceParseRegexInclusive();

    //[GeneratedRegex(@$"^{DICE_REGEX}$")]
    //private static partial Regex DiceParseRegexExclusive();

    private static Regex DiceParseRegexInclusive() => new(DICE_REGEX);

    private static Regex DiceParseRegexExclusive() => new($"^{DICE_REGEX}$");
    #endregion
}