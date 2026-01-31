using CardboardBox.Extensions;

using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Auth;

using Http;

public interface IOAuthService
{
	Task<TokenResponse?> ResolveCode(string code);
}

public class OAuthService(
	IApiService _api,
	IConfiguration _config) : IOAuthService
{
	public string AppId => _config["OAuth:AppId"] ?? throw new ArgumentNullException("OAuth:AppId");
	public string Secret => _config["OAuth:Secret"] ?? throw new ArgumentNullException("OAuth:Secret");
	public string OAuthUrl => _config["OAuth:Url"]?.ForceNull() ?? "https://auth.index-0.com";

	public Task<TokenResponse?> ResolveCode(string code)
	{
		var request = new TokenRequest(code, Secret, AppId);
		return _api.Post<TokenResponse, TokenRequest>($"{OAuthUrl.TrimEnd('/')}/api/data", request);
	}
}

public record class TokenRequest(
	[property: JsonPropertyName("Code")] string Code,
	[property: JsonPropertyName("Secret")] string Secret,
	[property: JsonPropertyName("AppId")] string AppId);

public class TokenUser
{
	[JsonPropertyName("nickname")]
	public string Nickname { get; set; } = string.Empty;

	[JsonPropertyName("avatar")]
	public string Avatar { get; set; } = string.Empty;

	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("provider")]
	public string Provider { get; set; } = string.Empty;

	[JsonPropertyName("providerId")]
	public string ProviderId { get; set; } = string.Empty;
}

public class TokenApp
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("icon")]
	public string Icon { get; set; } = string.Empty;

	[JsonPropertyName("background")]
	public string Background { get; set; } = string.Empty;
}

public class TokenResponse
{
	[JsonPropertyName("error")]
	public string? Error { get; set; }

	[JsonPropertyName("user")]
	public TokenUser User { get; set; } = new();

	[JsonPropertyName("app")]
	public TokenApp App { get; set; } = new();

	[JsonPropertyName("provider")]
	public string Provider { get; set; } = string.Empty;

	[JsonPropertyName("createdOn")]
	public DateTimeOffset CreatedOn { get; set; }
}
