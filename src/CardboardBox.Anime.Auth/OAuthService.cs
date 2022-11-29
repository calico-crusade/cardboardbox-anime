using CardboardBox.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Auth
{
	public interface IOAuthService
	{
		Task<TokenResponse?> ResolveCode(string code);
	}

	public class OAuthService : IOAuthService
	{
		private readonly IApiService _api;
		private readonly IConfiguration _config;

		public string AppId => _config["OAuth:AppId"];
		public string Secret => _config["OAuth:Secret"];

		public OAuthService(
			IApiService api,
			IConfiguration config)
		{
			_api = api;
			_config = config;
		}

		public Task<TokenResponse?> ResolveCode(string code)
		{
			var request = new TokenRequest(code, Secret, AppId);
			return _api.Post<TokenResponse, TokenRequest>("https://auth.index-0.com/api/data", request);
		}
	}

	public record class TokenRequest(string Code, string Secret, string AppId);

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
}
