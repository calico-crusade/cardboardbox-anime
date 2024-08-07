namespace CardboardBox.Anime.Bot.Commands;

public class DateCommands
{
    public const string DEFAULT_DATE_FORMAT = "YYYY-MM-DD hh:mm:ss +TZ";
    public const string EXAMPLE_DATE = "2024-10-31 13:55:22 +0200";

    public readonly Dictionary<string, string> _formats = new()
    {
        { "Short Time", "t" },
        { "Long Time", "T" },
        { "Short Date", "d" },
        { "Long Date", "D" },
        { "Short Date/Time", "f" },
        { "Long Date/Time", "F" },
        { "Relative", "R" }
    };

    public string GetFormat(string? format)
    {
        format ??= "Relative";
        return _formats.TryGetValue(format, out var _frm) ? _frm : "R";
    }

    [Command("date", $"Gets the discord timestamp for the given date (give me {DEFAULT_DATE_FORMAT})", Ephemeral = true)]
    public async Task GetDiscordDate(SocketSlashCommand cmd,
        [Option("The date", true)] string date,
        [Option("Format", false, "Short Time", "Long Time", "Short Date", "Long Date", "Short Date/Time", "Long Date/Time", "Relative")] string? format = null)
    {
        var formatString = GetFormat(format);

        if (!DateTime.TryParse(date, out var dt))
        {
            await cmd.RespondAsync($"Invalid date format. Please use {DEFAULT_DATE_FORMAT} - Example: {EXAMPLE_DATE}", ephemeral: true);
            return;
        }

        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timestamp = (long)(dt.ToUniversalTime() - epoch).TotalSeconds;
        var discordDate = $"<t:{timestamp}:{formatString}>";

        await cmd.RespondAsync($@"Here is the format: `{discordDate}` which ends up as: {discordDate}", ephemeral: true);
    }

    [Command("date-picker", $"Gets the discord timestamp for the given date using a date picker", Ephemeral = true)]
    public async Task GetDiscordDate(SocketSlashCommand cmd,
        [Option("Year", true)] long year,
        [Option("Month", true)] long month,
        [Option("Day", true)] long day,
        [Option("Hour (24 hour variant)", true)] long hour,
        [Option("Minute", true)] long minute,
        [Option("Second", false)] long? second,
        [Option("Optional TimeZone (UTC is used otherwise)", false, 
            "-12:00 / International Date Line West", "-11:00 / Coordinated Universal Time-11", 
            "-10:00 / Aleutian Islands / Hawaii", "-09:00 / Alaska", 
            "-08:00 / Pacific Time (US & Canada)", "-07:00 / Mountain Standard Time (US & Canada)", 
            "-06:00 / Central Standard Time (US & Canada)", "-05:00 / Eastern Standard Time (US & Canada)", 
            "-04:00 / Atlantic Time (Canada)", "-03:00 / SA Eastern Standard Time (Brasilia)",
            "-02:00 / Greenland Standard Time", "-01:00 / Azores / Cabo Verde Is", 
            "-00:00 / GMT / Coordinated Universal Time", "+01:00 / Central Europe Standard Time", 
            "+02:00 / East Europe Standard Time", "+03:00 / Russian Standard Time", 
            "+04:00 / Azerbaijan Standard Time", "+05:00 / Pakistan Standard Time", 
            "+06:00 / Central Asia Standard Time", "+07:00 / SE Asia Standard Time ", 
            "+08:00 / China Standard Time", "+09:00 / Tokyo Standard Time", 
            "+10:00 / AUS Eastern Standard Time", "+11:00 / Central Pacific Standard Time", 
            "+12:00 / New Zealand Standard Time")] string? timezone,
        [Option("Output Format", false, "Short Time", "Long Time", "Short Date", "Long Date", "Short Date/Time", "Long Date/Time", "Relative")] string? format)
    {
        var tz = TimeZoneInfo.Utc;
        if (timezone is not null)
        {
            var tzOffset = timezone.Split(' ')[0];
            var offset = TimeSpan.Parse(tzOffset);
            tz = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.BaseUtcOffset == offset) ?? TimeZoneInfo.Utc;
        }

        format = GetFormat(format);
        var dt = new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)(second ?? 0), DateTimeKind.Unspecified);
        dt = TimeZoneInfo.ConvertTimeToUtc(dt, tz);
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var timestamp = (long)(dt - epoch).TotalSeconds;
        var discordDate = $"<t:{timestamp}:{format}>";
        await cmd.RespondAsync($@"Here is the format: `{discordDate}` which ends up as: {discordDate}", ephemeral: true);
    }
}
