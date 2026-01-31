namespace CardboardBox.Anime.Database;

public class DbProfile : DbObjectInt
{
	[JsonPropertyName("username")]
	public string Username { get; set; } = "";

	[JsonPropertyName("avatar")]
	public string Avatar { get; set; } = "";

	[JsonPropertyName("platformId")]
	public string PlatformId { get; set; } = "";

	[JsonPropertyName("admin")]
	public bool Admin { get; set; } = false;

	[JsonPropertyName("canRead")]
	public bool CanRead { get; set; } = false;

	[JsonIgnore]
	public string Email { get; set; } = "";

	[JsonPropertyName("provider")]
	public string Provider { get; set; } = string.Empty;

	[JsonPropertyName("providerId")]
	public string ProviderId { get; set; } = string.Empty;

	[JsonPropertyName("settingsBlob")]
	public string SettingsBlob { get; set; } = string.Empty;

	[JsonPropertyName("uiApproval")]
	public bool UiApproval { get; set; } = false;
}
