using CardboardBox.Anime.Database;
using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Api;

public class AuthUserResponse
{
	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;

	[JsonPropertyName("id")]
	public long Id { get; set; }

	[JsonPropertyName("user")]
	public UserData User { get; set; } = new();

	public class UserData
	{
		[JsonPropertyName("roles")]
		public string[] Roles { get; set; } = Array.Empty<string>();

		[JsonPropertyName("nickname")]
		public string Nickname { get; set; } = string.Empty;

		[JsonPropertyName("avatar")]
		public string Avatar { get; set; } = string.Empty;

		[JsonPropertyName("id")]
		public string Id { get; set; } = string.Empty;

		[JsonPropertyName("email")]
		public string Email { get; set; } = string.Empty;

		[JsonPropertyName("settings")]
		public string Settings { get; set; } = string.Empty;

		public static implicit operator UserData(DbProfile profile)
		{
			return new UserData
			{
				Roles = profile.Admin ? new[] { "Admin" } : Array.Empty<string>(),
				Nickname = profile.Username,
				Id = profile.PlatformId,
				Email = profile.Email,
				Avatar = profile.Avatar,
				Settings = profile.SettingsBlob
			};
		}
	}
}
