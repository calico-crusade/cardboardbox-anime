namespace CardboardBox.Anime.Bot.Commands.Nsfw;

using Match;
using Services;

public class NsfwCommands
{
    private readonly IDbService _db;
    private readonly INsfwApiService _api;
    private readonly INsfwService _nsfw;
	private readonly DiscordSocketClient _client;

    public NsfwCommands(
        IDbService db,
        INsfwApiService api,
		INsfwService nsfw,
		DiscordSocketClient client)
    {
        _db = db;
        _api = api;
        _nsfw = nsfw;
        _client = client;
    }

    [Command("nsfw-check", "Checks if the given image is SFW or not", LongRunning = true)]
    public async Task Check(SocketSlashCommand cmd, [Option("Image URL")] string url)
    {
        var nsfw = await _api.Get(url);
        if (nsfw == null || !nsfw.Worked)
        {
            var error = nsfw?.Error ?? "The return results was empty!";
            await cmd.Modify($"Something went wrong while processing your request!\r\n{error}");
            return;
        }

        var classes = nsfw.Classifications
            .OrderByDescending(t => t.Probability)
            .Select(t => $"{t.Name}: {t.Probability * 100:00.00}%");

        var msg = string.Join("\r\n", classes);
        await cmd.Modify($"Here are the results:\r\n{msg}");
    }

    [Command("nsfw-config-show", "Shows the configuration for the guild specified", LongRunning = true)]
    public async Task ConfigShow(SocketSlashCommand cmd,
        [Option("Guild ID", false)] string? guildId,
        [Option("concise", false)] bool? concise = null)
    {
        concise ??= false;

		var (config, guild, _, admin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !admin) return;

		if (concise ?? false)
        {
            await cmd.Modify(_nsfw.FormatConcise(config));
            return;
        }

		await cmd.Modify(_nsfw.Format(config)
			.WithAuthor(guild.Name, guild.IconUrl)
			.WithTitle("NSFW Image Detection!"));
    }

    [Command("nsfw-config-init", "Configures the NSFW detection for the guild specified", LongRunning = true)]
    public async Task ConfigInit(SocketSlashCommand cmd, [Option("Guild ID", false)] string? guildId)
    {
		var (config, guild, _, admin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !admin) return;

		await cmd.Modify(_nsfw.Format(config)
            .WithAuthor(guild.Name, guild.IconUrl)
			.WithTitle("NSFW Image Detection!"));
    }

    [Command("nsfw-config-classify", "Sets the classification threshold for taking action. 0 disables it.", LongRunning = true)]
    public async Task ConfigClassify(SocketSlashCommand cmd,
        [Option("Guild ID", false)] string? guildId,
        [Option("Classification Type", "Porn", "Hentai", "Sexual")] string type,
        [Option("Threshold Percentage (0 - 100)")] long percentage)
    {
        var types = new[] { "Porn", "Hentai", "Sexual" };
        var (config, guild, _, admin) = await _nsfw.GetAdmin(cmd, guildId);
        if (config == null || guild == null || !admin) return;

        if (percentage < 0 || percentage > 100)
        {
            await cmd.Modify("Invalid percentage. It has to be between 1 or 100. 0 disables the classification.");
            return;
        }

        if (!types.Contains(type))
        {
            await cmd.Modify("Invalid classifiction type. It has to be one of: " + string.Join(", ", types));
			return;
        }

        switch (type)
        {
            case "Porn": config.ClassifyPorn = (int)percentage; break;
            case "Hentai": config.ClassifyHentai = (int)percentage; break;
            case "Sexual": config.ClassifySexy = (int)percentage; break;
        }

        await _db.Nsfw.Upsert(config);
        await cmd.Modify("Done! ✅");
    }

    [Command("nsfw-config-actions", "Configures what the bot will do when someone posts NSFW content", LongRunning = true)]
    public async Task ConfigActions(SocketSlashCommand cmd,
		[Option("Guild ID", false)] string? guildId,
        [Option("Kick After X Infractions (0 to disable)", false)] long? kick,
        [Option("Ban After X Infractions (0 to disable)", false)] long? ban,
        [Option("Delete Messages", false)] bool? delete,
        [Option("Ingore NSFW / Age Restricted Channels", false)] bool? nsfw)
    {
		var (config, guild, _, admin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !admin) return;

        if (kick == null && ban == null && delete == null && nsfw == null)
        {
            await cmd.Modify("Please specify what you want to change.");
            return;
        }

        if (kick != null)
        {
            if (kick < 0 || kick > 100)
            {
                await cmd.Modify("Invalid Kick Infraction config. It needs to be between 0 and 100.");
                return;
            }
            config.KickAfter = (int)kick.Value;
        }

        if (ban != null)
		{
			if (ban < 0 || ban > 100)
			{
				await cmd.Modify("Invalid Ban Infraction config. It needs to be between 0 and 100.");
				return;
			}
			config.BanAfter = (int)ban.Value;
		}

        if (delete != null) config.DeleteMessage = delete.Value;
        if (nsfw != null) config.IgnoreNsfwChannels = nsfw.Value;

        await _db.Nsfw.Upsert(config);
		await cmd.Modify("Done! ✅");
	}

    [Command("nsfw-config-ignore-channel", "Configures whether the bot ignores certain channels.", LongRunning = true)]
    public async Task ConfigIgnoreChannel(SocketSlashCommand cmd,
		[Option("Guild ID", false)] string? guildId,
        [Option("Channel in question", false)] IGuildChannel? channel,
		[Option("Channel ID in question", false)] string? id,
		[Option("Whether to ignore the channel or not")] bool ignore)
    {
		var (config, guild, _, admin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !admin) return;

        if ((channel == null && string.IsNullOrEmpty(id)) ||
            (channel != null && !string.IsNullOrEmpty(id)))
		{
			await cmd.Modify("Invalid channel. Please specify either the Channel or the Channel ID, but not both");
			return;
		}

        var channelId = channel == null && ulong.TryParse(id, out var cid) ? cid : channel?.Id;
        if (channelId == null)
		{
			await cmd.Modify("Invalid channel. Please specify either the Channel or the Channel ID, but not both");
			return;
		}

        var inCol = config.IgnoreChannels.Contains(channelId.Value);
        if ((inCol && ignore) || (!inCol && !ignore))
        {
            await cmd.Modify("There would be no change.");
            return;
        }

        config.IgnoreChannels = inCol ? 
            config.IgnoreChannels.Where(t => t != channelId).ToArray() :
            config.IgnoreChannels.Append(channelId.Value).ToArray();
        await _db.Nsfw.Upsert(config);
		await cmd.Modify("Done! ✅");
	}

    [Command("nsfw-config-ignore-role", "Configures whether to bot ignores certain roles.", LongRunning = true)]
    public async Task ConfigIgnoreRole(SocketSlashCommand cmd,
        [Option("Guild ID", false)] string? guildId,
        [Option("Role in question", false)] IRole? role,
        [Option("Role ID in question", false)] string? id,
        [Option("Whether to ignore the role or not")] bool ignore)
    {
		var (config, guild, _, admin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !admin) return;

        if ((role == null && string.IsNullOrEmpty(id)) ||
            (role != null && !string.IsNullOrEmpty(id)))
        {
            await cmd.Modify("Invalid role. Please specify either the Role or the Role ID, but not both");
            return;
        }

        var roleId = role == null && ulong.TryParse(id, out var rid) ? rid : role?.Id;
        if (roleId == null)
		{
			await cmd.Modify("Invalid role. Please specify either the Role or the Role ID, but not both");
			return;
		}

        var inCol = config.AllowedRoles.Contains(roleId.Value);
		if ((inCol && ignore) || (!inCol && !ignore))
		{
			await cmd.Modify("There would be no change.");
			return;
		}

        config.AllowedRoles = inCol ?
            config.AllowedRoles.Where(t => t != roleId).ToArray() :
            config.AllowedRoles.Append(roleId.Value).ToArray();
        await _db.Nsfw.Upsert(config);
		await cmd.Modify("Done! ✅");
	}

	[Command("nsfw-config-admin", "Configures the roles that are allowed to change the config.", LongRunning = true)]
	public async Task ConfigAdmin(SocketSlashCommand cmd,
		[Option("Guild ID", false)] string? guildId,
		[Option("Role in question", false)] IRole? role,
		[Option("Role ID in question", false)] string? id,
		[Option("Whether to the role is an admin or not")] bool admin)
	{
		var (config, guild, _, isAdmin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !isAdmin) return;

		if ((role == null && string.IsNullOrEmpty(id)) ||
			(role != null && !string.IsNullOrEmpty(id)))
		{
			await cmd.Modify("Invalid role. Please specify either the Role or the Role ID, but not both");
			return;
		}

		var roleId = role == null && ulong.TryParse(id, out var rid) ? rid : role?.Id;
		if (roleId == null)
		{
			await cmd.Modify("Invalid role. Please specify either the Role or the Role ID, but not both");
			return;
		}

		var inCol = config.AllowedRoles.Contains(roleId.Value);
		if ((inCol && admin) || (!inCol && !admin))
		{
			await cmd.Modify("There would be no change.");
			return;
		}

		config.AdminRoles = inCol ?
			config.AdminRoles.Where(t => t != roleId).ToArray() :
			config.AdminRoles.Append(roleId.Value).ToArray();
		await _db.Nsfw.Upsert(config);
		await cmd.Modify("Done! ✅");
	}

    [Command("nsfw-config-enable", "Enables or disables NSFW detection", LongRunning = true)]
    public async Task ConfigEnable(SocketSlashCommand cmd,
        [Option("Guild ID", false)] string? guildId,
        [Option("Enabled")] bool enable)
    {
		var (config, guild, _, isAdmin) = await _nsfw.GetAdmin(cmd, guildId);
		if (config == null || guild == null || !isAdmin) return;

        config.Enabled = enable;
		await _db.Nsfw.Upsert(config);
		await cmd.Modify("Done! ✅");
	}
}
