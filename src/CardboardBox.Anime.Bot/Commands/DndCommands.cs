namespace CardboardBox.Anime.Bot.Commands;

using Dnd;
using Dnd.NumberGenerator;

public class DndCommands
{
    private readonly INumberGenerator _generator = new CryptoNumberGenerator();

    private const int MAX_DICE_SHOWN = 20;

    public static Dictionary<DamageType, string> IconMap()
    {
        return new()
        {
            [DamageType.Unknown] = ":game_die:",
            [DamageType.Acid] = ":test_tube:",
            [DamageType.Bludgeoning] = ":hammer:",
            [DamageType.Cold] = ":snowflake:",
            [DamageType.Fire] = ":fire:",
            [DamageType.Force] = ":rightwards_pushing_hand:",
            [DamageType.Lightning] = ":zap:",
            [DamageType.Necrotic] = ":zombie:",
            [DamageType.Piercing] = ":bow_and_arrow:",
            [DamageType.Poison] = ":nauseated_face:",
            [DamageType.Psychic] = ":milky_way:",
            [DamageType.Radiant] = ":sparkles:",
            [DamageType.Slashing] = ":crossed_swords:",
            [DamageType.Thunder] = ":thunder_cloud_rain:",
        };
    }

    [Command("roll", "Roll a D&D formatted dice (example: 3D10+2)")]
    public async Task DndRoll(SocketSlashCommand cmd,
        [Option("The dice to roll", true)] string input,
        [Option("Roll type", false, "Regular", "Advantage", "Disadvantage")] string? type)
    {
        if (!Roll.TryParse(input, out var roll))
        {
            await cmd.RespondAsync("Invalid roll format. Please use the format like `3D10+2, 4D10-2 (radiant)`.");
            return;
        }

        if (!Enum.TryParse<RollType>(type, out var rollType))
            rollType = RollType.Regular;

        var results = roll.Results(rollType, _generator);

        var bob = new StringBuilder();
        bob.Append($"**Roll:** `{roll}`");

        if (rollType != RollType.Regular)
            bob.Append($" With {rollType}");

        bob.AppendLine();

        var dice = results.OrderBy(t => t.Dice.Type).ThenByDescending(t => t.Dice.Sides);
        foreach (var die in dice)
        {
            var dieRoll = die.Dice.ToString();
            foreach (var (dt, icon) in IconMap())
                dieRoll = dieRoll.Replace($"({dt.ToString().ToLower()})", icon);

            bob.Append($"- {dieRoll} = **{die.Total}**");

            if (die.Rolls.Length > 1 && die.Rolls.Length <= MAX_DICE_SHOWN)
                bob.Append($" [{string.Join("+", die.Rolls)}]");

            if (die.Dice.Modifier > 0)
                bob.Append($" +{die.Dice.Modifier}");

            if (rollType != RollType.Regular)
                bob.Append($" / ~~{die.AlternateTotal}~~");

            if (die.AlternateRolls.Length > 1 &&
                die.Rolls.Length <= MAX_DICE_SHOWN &&
                rollType != RollType.Regular)
                bob.Append($" [{string.Join("+", die.AlternateRolls)}]");

            bob.AppendLine();
        }

        bob.AppendLine($"**Total:** {results.Total}");
        var totals = string.Join("\r\n", results.GroupBy(t => t.Dice.Type)
            .Select(t =>
            {
                var total = t.Sum(r => r.Total);
                var min = t.Sum(r => r.Dice.Min);
                var max = t.Sum(r => r.Dice.Max);
                var avg = ((min + max) / 2.0).ToString("0.0");
                var key = IconMap()[t.Key];
                return $"- **{total}** {key} (Avg: {avg}, Min: {min}, Max: {max})";
            }));
        bob.AppendLine(totals);

        var output = bob.ToString().Trim();
        await cmd.RespondAsync(output);
    }
}
