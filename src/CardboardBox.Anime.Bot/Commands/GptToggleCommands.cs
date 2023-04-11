namespace CardboardBox.Anime.Bot.Commands;

using Match;
using Services;

public class GptToggleCommands
{
	private readonly IDbService _db;
	private readonly INsfwApiService _nsfw;
	
	public GptToggleCommands(IDbService db, INsfwApiService nsfw)
	{
		_db = db;
		_nsfw = nsfw;
	}

	[GuildCommand("gpt-check", "Checks whether or not a guild or user is authorized to use the ChatGPT api", 
		HolybookCommands.CARDBOARD_BOX_SERVER, HolybookCommands.CARDBOARD_BOX_ROLE, LongRunning = true)]
	public async Task GptCheck(SocketSlashCommand cmd,
		[Option("User Id", false)] string? userId,
		[Option("Guild Id", false)] string? guildId)
	{
		bool userS = string.IsNullOrEmpty(userId),
			 guildS = string.IsNullOrEmpty(guildId);

		if (userS && guildS)
		{
			await cmd.Modify("Please specify at least the User Id or Guild Id");
			return;
		}

		if (userS && !guildS)
		{
			var guild = await _db.Gpt.FetchGroup(guildId ?? "");
			if (guild == null)
			{
				await cmd.Modify("Guild has not been listed.");
				return;
			}

			await cmd.Modify("Guild is on the " + guild.Type);
			return;
		}

		if (!userS && guildS)
		{
			var user = await _db.Gpt.FetchUser(userId ?? "");
			if (user == null)
			{
				await cmd.Modify("User has not been listed.");
				return;
			}

			await cmd.Modify("User is on the " + user.Type);
			return;
		}

		var marks = await _db.Gpt.Get(userId ?? "", guildId ?? "");
		if (marks == null || marks.Length == 0)
		{
			await cmd.Modify("User & Guild are not listed anywhere.");
			return;
		}

		var bob = new StringBuilder();
		foreach(var item in marks)
		{
			if (string.IsNullOrEmpty(item.UserId) && string.IsNullOrEmpty(item.ServerId)) continue;

			if (string.IsNullOrEmpty(item.UserId) && !string.IsNullOrEmpty(item.ServerId))
			{
				bob.AppendLine($"Guild is on the " + item.Type);
				continue;
			}

			if (!string.IsNullOrEmpty(item.UserId) && string.IsNullOrEmpty(item.ServerId))
			{
				bob.AppendLine($"User is on the " + item.Type);
				continue;
			}

			bob.AppendLine($"User + Guild is on the " + item.Type);
		}

		await cmd.Modify($"Results:\r\n```{bob}```");
	}

	[GuildCommand("gpt-toggle", "Toggles whether or not a user/guild can use the ChatGPT api", 
		HolybookCommands.CARDBOARD_BOX_SERVER, HolybookCommands.CARDBOARD_BOX_ROLE, LongRunning = true)]
	public async Task GptToggle(SocketSlashCommand cmd,
		[Option("User Id", false)] string? userId,
		[Option("Guild Id", false)] string? guildId,
		[Option("Type", true, GptAuthorized.WHITE_LIST, GptAuthorized.BLACK_LIST)] string type)
	{
		bool userS = string.IsNullOrEmpty(userId),
			 guildS = string.IsNullOrEmpty(guildId);

		if (userS && guildS)
		{
			await cmd.Modify("Please specify at least the User Id or Guild Id");
			return;
		}

		if (!userS && guildS)
		{
			await _db.Gpt.ToggleUser(userId ?? "", type);
			await cmd.Modify("Done");
			return;
		}

		if (userS && !guildS)
		{
			await _db.Gpt.ToggleGroup(guildId ?? "", type);
			await cmd.Modify("Done?");
			return;
		}

		await _db.Gpt.Toggle(userId ?? "", guildId ?? "", type);
		await cmd.Modify("Done!");
	}

	[GuildCommand("gpt-validate", "Toggles whether or not a user/guild can use the ChatGPT api",
		HolybookCommands.CARDBOARD_BOX_SERVER, HolybookCommands.CARDBOARD_BOX_ROLE, LongRunning = true)]
	public async Task GptValidate(SocketSlashCommand cmd,
		[Option("User Id")] string user,
		[Option("Guild Id", false)] string? guild)
	{
		ulong? guildId = null;
		if (!ulong.TryParse(user, out ulong userId))
		{
			await cmd.Modify("Invalid user id");
			return;
		}

		if (!string.IsNullOrEmpty(guild) &&
			ulong.TryParse(guild, out var t)) guildId = t;

		var validate = await _db.Gpt.ValidateUser(userId, guildId, Array.Empty<ulong>());
		await cmd.Modify($"User is {(validate ? "" : "un")}authoried");
	}

}
