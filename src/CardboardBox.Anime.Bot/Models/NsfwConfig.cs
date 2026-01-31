namespace CardboardBox.Anime.Bot;

public class NsfwConfig : DbObjectInt
{
	public string GuildId { get; set; } = string.Empty;

	public bool Enabled { get; set; } = false;

	public bool IgnoreNsfwChannels { get; set; } = true;

	public string[] IgnoreChannels { get; set; } = Array.Empty<string>();

	public string[] AllowedRoles { get; set; } = Array.Empty<string>();

	public string[] AdminRoles { get; set; } = Array.Empty<string>();

	public string? LogChannelId { get; set; }

	public int ClassifyHentai { get; set; } = 60;

	public int ClassifySexy { get; set; } = 0;

	public int ClassifyPorn { get; set; } = 60;

	public bool DeleteMessage { get; set; } = true;

	public int KickAfter { get; set; } = 3;

	public int BanAfter { get; set; } = 5;
}

public class NsfwConfigState : DbObjectInt
{
	public ulong GuildId { get; set; }

	public bool Enabled { get; set; } = false;

	public bool IgnoreNsfwChannels { get; set; } = true;

	public ulong[] IgnoreChannels { get; set; } = Array.Empty<ulong>();

	public ulong[] AllowedRoles { get; set; } = Array.Empty<ulong>();

	public ulong[] AdminRoles { get; set; } = Array.Empty<ulong>();

	public ulong? LogChannelId { get; set; }

	public int ClassifyHentai { get; set; } = 60;

	public int ClassifySexy { get; set; } = 0;

	public int ClassifyPorn { get; set; } = 60;

	public bool DeleteMessage { get; set; } = true;

	public int KickAfter { get; set; } = 3;

	public int BanAfter { get; set; } = 5;

	public static implicit operator NsfwConfig(NsfwConfigState state)
	{
		var convert = (ulong[] items) => items.Select(t => t.ToString()).ToArray();

		return new()
		{
			Id = state.Id,
			GuildId = state.GuildId.ToString(),
			Enabled = state.Enabled,
			IgnoreNsfwChannels = state.IgnoreNsfwChannels,
			IgnoreChannels = convert(state.IgnoreChannels),
			AllowedRoles = convert(state.AllowedRoles),
			AdminRoles = convert(state.AdminRoles),
			ClassifyHentai = state.ClassifyHentai,
			ClassifyPorn = state.ClassifyPorn,
			ClassifySexy = state.ClassifySexy,
			DeletedAt = state.DeletedAt,
			CreatedAt = state.CreatedAt,
			UpdatedAt = state.UpdatedAt,

			DeleteMessage = state.DeleteMessage,
			KickAfter = state.KickAfter,
			BanAfter = state.BanAfter,
			LogChannelId = state.LogChannelId?.ToString(),
		};
	}

	public static implicit operator NsfwConfigState(NsfwConfig state)
	{
		var convert = (string[] items) => items.Select(t => ulong.Parse(t)).ToArray();

		return new()
		{
			Id = state.Id,
			GuildId = ulong.Parse(state.GuildId),
			Enabled = state.Enabled,
			IgnoreNsfwChannels = state.IgnoreNsfwChannels,
			IgnoreChannels = convert(state.IgnoreChannels),
			AllowedRoles = convert(state.AllowedRoles),
			AdminRoles = convert(state.AdminRoles),
			ClassifyHentai = state.ClassifyHentai,
			ClassifyPorn = state.ClassifyPorn,
			ClassifySexy = state.ClassifySexy,
			DeletedAt = state.DeletedAt,
			CreatedAt = state.CreatedAt,
			UpdatedAt = state.UpdatedAt,

			DeleteMessage = state.DeleteMessage,
			KickAfter = state.KickAfter,
			BanAfter = state.BanAfter,
			LogChannelId = string.IsNullOrEmpty(state.LogChannelId) ? null : ulong.Parse(state.LogChannelId),
		};
	}
}