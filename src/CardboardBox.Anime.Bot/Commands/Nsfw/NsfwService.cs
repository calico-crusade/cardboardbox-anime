namespace CardboardBox.Anime.Bot.Commands.Nsfw;

using Match;
using Services;

public interface INsfwService
{
	Task<SocketGuild?> GetGuild(SocketSlashCommand cmd, string? overrider = null);

	Task<SocketGuildUser?> GetUser(SocketSlashCommand cmd, SocketGuild guild, ulong? userId = null);

	Task<SocketGuildUser?> GetUser(SocketSlashCommand cmd, ulong? userId = null, string? overrider = null);

	Task<bool> ValidateAdminUser(SocketSlashCommand cmd, SocketGuildUser user, NsfwConfigState? config = null);

	Task<bool> ValidateAdminUser(SocketSlashCommand cmd, SocketGuild guild, ulong? userId = null, NsfwConfigState? config = null);

	Task<bool> ValidateAdminUser(SocketSlashCommand cmd, string? overrider = null, ulong? userId = null, NsfwConfigState? config = null);

	bool TryGetGuild(SocketSlashCommand cmd, out ulong guildId);

	bool TryGetGuild(SocketSlashCommand cmd, string? overrider, out ulong guildId);

	string FormatConcise(NsfwConfigState config);

	EmbedBuilder Format(NsfwConfigState config);

	Task<NsfwConfigState?> GetConfig(SocketSlashCommand cmd, string? overrider = null);

	Task<NsfwConfigState?> GetConfig(SocketSlashCommand cmd, SocketGuild guild);

	Task<(NsfwConfigState? state, SocketGuild? guild, SocketGuildUser? user)> Get(SocketSlashCommand cmd, string? overrider = null, ulong? userId = null);

	Task<(NsfwConfigState? state, SocketGuild? guild, SocketGuildUser? user, bool admin)> GetAdmin(SocketSlashCommand cmd, string? overrider = null, ulong? userId = null);
}

public class NsfwService : INsfwService
{
	public static ulong[] AUTHORIZED_USERS = { 191100926486904833 };

	private readonly IDbService _db;
	private readonly INsfwApiService _api;
	private readonly DiscordSocketClient _client;

	public NsfwService(
		IDbService db, 
		INsfwApiService api, 
		DiscordSocketClient client)
	{
		_db = db;
		_api = api;
		_client = client;
	}

	public async Task<SocketGuild?> GetGuild(SocketSlashCommand cmd, string? overrider = null)
	{
		if (!TryGetGuild(cmd, overrider, out var id))
		{
			await cmd.Modify("Invalid guild! Please specify a guild ID correctly!");
			return null;
		}

		var guild = _client.GetGuild(id);
		if (guild == null)
		{
			await cmd.Modify("I'm not in that guild!");
			return null;
		}

		return guild;
	}

	public async Task<SocketGuildUser?> GetUser(SocketSlashCommand cmd, SocketGuild guild, ulong? userId = null)
	{
		userId ??= cmd.User.Id;

		var user = guild.GetUser(userId.Value);
		if (user == null)
		{
			await cmd.Modify($"I couldn't find user `{userId}` in `{guild.Name} ({guild.Id})`");
			return null;
		}

		return user;
	}

	public async Task<SocketGuildUser?> GetUser(SocketSlashCommand cmd, ulong? userId = null, string? overrider = null)
	{
		var guild = await GetGuild(cmd, overrider);
		if (guild == null) return null;

		return await GetUser(cmd, guild, userId);
	}

	public async Task<bool> ValidateAdminUser(SocketSlashCommand cmd, SocketGuildUser user, NsfwConfigState? config = null)
	{
		if (AUTHORIZED_USERS.Contains(user.Id) || user.GuildPermissions.Administrator) return true;
		if (config != null && user.Roles.Any(t => config.AdminRoles.Contains(t.Id))) return true;

		await cmd.Modify("You don't have permission to access this!");
		return false;
	}

	public async Task<bool> ValidateAdminUser(SocketSlashCommand cmd, SocketGuild guild, ulong? userId = null, NsfwConfigState? config = null)
	{
		var user = await GetUser(cmd, guild, userId);
		if (user == null) return false;

		return await ValidateAdminUser(cmd, user, config);
	}

	public async Task<bool> ValidateAdminUser(SocketSlashCommand cmd, string? overrider = null, ulong? userId = null, NsfwConfigState? config = null)
	{
		var user = await GetUser(cmd, userId, overrider);
		if (user == null) return false;

		return await ValidateAdminUser(cmd, user, config);
	}

	public bool TryGetGuild(SocketSlashCommand cmd, out ulong guildId) => TryGetGuild(cmd, null, out guildId);

	public bool TryGetGuild(SocketSlashCommand cmd, string? overrider, out ulong guildId)
	{
		guildId = 0;
		if (!string.IsNullOrEmpty(overrider))
			return ulong.TryParse(overrider.Trim(), out guildId);

		if (cmd.Channel is not SocketGuildChannel chnl)
			return false;

		guildId = chnl.Guild.Id;
		return true;
	}

	public string FormatConcise(NsfwConfigState config)
	{
		var allowRoles = string.Join(", ", config.AllowedRoles.Select(x => $"<@&{x}>"));
		var ignoreChan = string.Join(", ", config.IgnoreChannels.Select(x => $"<#{x}>"));
		var adminRoles = string.Join(", ", config.AdminRoles.Select(x => $"<@&{x}>"));

		return $@"Enabled: {config.Enabled}
Ignore AR Channels: {config.IgnoreNsfwChannels}
Hentai Threshold: {config.ClassifyHentai}%
Pornography Threshold: {config.ClassifyPorn}%
Sexual Threshold: {config.ClassifySexy}%
Delete Messages: {config.DeleteMessage}
Kick After: {config.KickAfter}
Ban After: {config.BanAfter}
Ignored Channels: {ignoreChan}
Ignored Roles: {allowRoles}
Admin Roles: {adminRoles}
Log Channel: {(config.LogChannelId == null ? "" : $"<#{config.LogChannelId}>")}";
	}

	public EmbedBuilder Format(NsfwConfigState config)
	{
		var allowRoles = string.Join(", ", config.AllowedRoles.Select(x => $"<@&{x}>"));
		var ignoreChan = string.Join(", ", config.IgnoreChannels.Select(x => $"<#{x}>"));
		var adminRoles = string.Join(", ", config.AdminRoles.Select(x => $"<@&{x}>"));

		var bob = new EmbedBuilder();

		bob.WithDescription($"I will {(config.Enabled ? "**not** " : "")}ignore NSFW content. " +
			$"Below are the different classifications of NSFW content I have along with the probability I will take action for and various other settings.");

		bob.AddField("Hentai", config.ClassifyHentai == 0 ? "Ignored." : $"{config.ClassifyHentai}%+", true)
		   .AddField("Pornography", config.ClassifyPorn == 0 ? "Ignored." : $"{config.ClassifyPorn}%+", true)
		   .AddField("Sexual", config.ClassifySexy == 0 ? "Ignored." : $"{config.ClassifySexy}%+", true);

		bob.AddField("Age Restricted Channels", config.IgnoreNsfwChannels ? "Ignored" : "Not Ignored");

		bob.AddField("Message Deletion", config.DeleteMessage ? "Enabled" : "Disabled", true)
		   .AddField("Kick After", config.KickAfter == 0 ? "Disabled" : config.KickAfter + " Infractions", true)
		   .AddField("Ban After", config.BanAfter == 0 ? "Disabled" : config.BanAfter + " Infractions", true);


		if (config.IgnoreChannels.Any())
			bob.AddField("Ignored Channels", ignoreChan);
		if (config.AllowedRoles.Any())
			bob.AddField("Ignored Roles", allowRoles);
		if (config.AdminRoles.Any())
			bob.AddField("Admin Roles (These roles can change settings)", adminRoles);

		if (config.LogChannelId != null)
			bob.AddField("Log Channel", $"<#{config.LogChannelId}>");

		return bob;
	}

	public async Task<NsfwConfigState?> GetConfig(SocketSlashCommand cmd, string? overrider = null)
	{
		var guild = await GetGuild(cmd, overrider);
		if (guild == null) return null;

		return await GetConfig(cmd, guild);
	}

	public async Task<NsfwConfigState?> GetConfig(SocketSlashCommand cmd, SocketGuild guild)
	{
		var config = await _db.Nsfw.Fetch(guild.Id);
		if (config == null)
		{
			await _db.Nsfw.Upsert(new NsfwConfigState { GuildId = guild.Id });
			config = await _db.Nsfw.Fetch(guild.Id);
		}

		if (config == null)
		{
			await cmd.Modify("Something went wrong while creating the config!");
			return null;
		}

		return config;
	}

	public async Task<(NsfwConfigState? state, SocketGuild? guild, SocketGuildUser? user)> Get(SocketSlashCommand cmd, string? overrider = null, ulong? userId = null)
	{
		var guild = await GetGuild(cmd, overrider);
		if (guild == null) return (null, null, null);

		var config = await GetConfig(cmd, guild);
		if (config == null) return (null, null, null);

		var user = await GetUser(cmd, guild, userId);
		if (user == null) return (null, null, null);

		return (config, guild, user);
	}

	public async Task<(NsfwConfigState? state, SocketGuild? guild, SocketGuildUser? user, bool admin)> GetAdmin(SocketSlashCommand cmd, string? overrider = null, ulong? userId = null)
	{
		var (config, guild, user) = await Get(cmd, overrider, userId);
		if (guild == null || config == null || user == null) return (null, null, null, false);

		var admin = await ValidateAdminUser(cmd, user, config);
		return (config, guild, user, admin);
	}
}
