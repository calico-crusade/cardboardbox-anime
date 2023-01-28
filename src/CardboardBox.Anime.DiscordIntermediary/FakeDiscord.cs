using AutoMapper;
using Discord;
using Discord.Rest;

namespace CardboardBox.Anime.DiscordIntermediary;

public static class FakeExtensions
{
	private static MapperConfiguration? _config;

	public static T Convert<T>(object from)
	{
		_config ??= new MapperConfiguration(c =>
		{
			c.CreateMap<RestGuild, FakeGuild>();
			c.CreateMap<RestUser, FakeUser>();
			c.CreateMap<RestTextChannel, FakeChannel>();
			c.CreateMap<RestRole, FakeRole>();
			c.CreateMap<RestGuildUser, FakeGuildUser>();
		});
		return _config.CreateMapper().Map<T>(from);
	}
}

public class FakeRole
{
	public Color Color { get; set; }
	public bool IsHoisted { get; set; }
	public bool IsManaged { get; set; }
	public bool IsMentionable { get; set; }
	public string Name { get; set; } = string.Empty;
	public Emoji? Emoji { get; set; }
	public int Position { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public bool IsEveryone { get; set; }
	public string Mention { get; set; } = string.Empty;
	public string Id { get; set; } = string.Empty;

	public static implicit operator FakeRole(RestRole user)
	{
		return FakeExtensions.Convert<FakeRole>(user);
	}
}

public class FakeChannel
{
	public string Topic { get; set; } = string.Empty;
	public int SlowModeInterval { get; set; }
	public string CategoryId { get; set; } = string.Empty;
	public string Mention { get; set; } = string.Empty;
	public bool IsNsfw { get; set; }
	public string Name { get; set; } = string.Empty;
	public int Position { get; set; }
	public string GuildId { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
	public string Id { get; set; } = string.Empty;

	public static implicit operator FakeChannel(RestChannel user)
	{
		return FakeExtensions.Convert<FakeChannel>(user);
	}
}

public class FakeUser
{
	public bool IsBot { get; set; }
	public string Username { get; set; } = string.Empty;
	public int DiscriminatorValue { get; set; }
	public string AvatarId { get; set; } = string.Empty;
	public string BannerId { get; set; } = string.Empty;

	public Color AccentColor { get; set; }
	public int PublicFlags { get; set; }
	public DateTimeOffset CreatedAt { get; set; }
	public string Discriminator { get; set; } = string.Empty;

	public string Mention { get; set; } = string.Empty;
	public string Id { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public static implicit operator FakeUser(RestUser user)
	{
		return FakeExtensions.Convert<FakeUser>(user);
	}
}

public class FakeGuildUser : FakeUser
{
	public string Nickname { get; set; } = string.Empty;
	public string[] RoleIds { get; set; } = Array.Empty<string>();

	public static implicit operator FakeGuildUser(RestGuildUser user)
	{
		return FakeExtensions.Convert<FakeGuildUser>(user);
	}
}

public class FakeGuild
{
	public FakeRole[] Roles { get; set; } = Array.Empty<FakeRole>();
	public GuildEmote[] Emotes { get; set; } = Array.Empty<GuildEmote>();

	public string Name { get; private set; } = string.Empty;

	public string Id { get; set; } = string.Empty;

	public int AFKTimeout { get; private set; }

	public bool IsWidgetEnabled { get; private set; }

	public VerificationLevel VerificationLevel { get; private set; }

	public MfaLevel MfaLevel { get; private set; }

	public DefaultMessageNotifications DefaultMessageNotifications { get; private set; }

	public ExplicitContentFilterLevel ExplicitContentFilter { get; private set; }

	public string AFKChannelId { get; private set; } = string.Empty;

	public string WidgetChannelId { get; private set; } = string.Empty;

	public string SystemChannelId { get; private set; } = string.Empty;

	public string RulesChannelId { get; private set; } = string.Empty;

	public string PublicUpdatesChannelId { get; private set; } = string.Empty;

	public string OwnerId { get; private set; } = string.Empty;

	public string VoiceRegionId { get; private set; } = string.Empty;

	public string IconId { get; private set; } = string.Empty;

	public string SplashId { get; private set; } = string.Empty;

	public string DiscoverySplashId { get; private set; } = string.Empty;

	internal bool Available { get; private set; }

	public string ApplicationId { get; private set; } = string.Empty;

	public PremiumTier PremiumTier { get; private set; }

	public string BannerId { get; private set; } = string.Empty;

	public string VanityURLCode { get; private set; } = string.Empty;

	public SystemChannelMessageDeny SystemChannelFlags { get; private set; }

	public string Description { get; private set; } = string.Empty;

	public int PremiumSubscriptionCount { get; private set; }

	public string PreferredLocale { get; private set; } = string.Empty;

	public int MaxPresences { get; private set; }

	public int MaxMembers { get; private set; }

	public int MaxVideoChannelUsers { get; private set; }

	public int ApproximateMemberCount { get; private set; }

	public int ApproximatePresenceCount { get; private set; }

	public NsfwLevel NsfwLevel { get; set; }

	public GuildFeatures? Features { get; set; }

	public static implicit operator FakeGuild(RestGuild guild)
	{
		return FakeExtensions.Convert<FakeGuild>(guild);
	}
}