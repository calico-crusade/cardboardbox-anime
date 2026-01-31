using CardboardBox.Anime.Auth.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CardboardBox.Anime.Auth;

internal class AuthMiddlewareOptions : AuthenticationSchemeOptions { }

internal class AuthMiddleware(
	IOptionsMonitor<AuthMiddlewareOptions> options,
	ILoggerFactory factory,
	UrlEncoder encoder,
	ILogger<AuthMiddleware> _logger,
	IJwtTokenService _jwt) : AuthenticationHandler<AuthMiddlewareOptions>(options, factory, encoder)
{
	/// <summary>
	/// The name of the custom scheme
	/// </summary>
	public const string SCHEMA = "cba-auth";

	/// <summary>
	/// The headers that will be checked for the authentication key
	/// </summary>
	public readonly string[] ClientHeaders = ["authorization", "access-token"];

	/// <summary>
	/// The headers that will be checked for API key requests
	/// </summary>
	public readonly string[] ApiHeaders = ["x-api-key", "api-key"];

	/// <summary>
	/// The prefixes that will be stripped from the authentication key
	/// </summary>
	public readonly string[] Prefixes = ["bearer", "key"];

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		try
		{
			var (key, client) = GetKey();
			if (string.IsNullOrEmpty(key))
				return AuthenticateResult.NoResult();

			if (client)
				return await HandleClient(key);

			return await HandleApi(key);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error while authenticating request");
			return AuthenticateResult.Fail(ex);
		}
	}

	/// <summary>
	/// Handle client requests
	/// </summary>
	/// <param name="key">The client authentication token</param>
	/// <returns>The result of the authentication request</returns>
	public async Task<AuthenticateResult> HandleClient(string key)
	{
		var token = await _jwt.ParseToken(key);
		if (token is null)
			return AuthenticateResult.Fail("Invalid JWT token");

		var identity = new ClaimsIdentity(token, SCHEMA);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, SCHEMA);
		return AuthenticateResult.Success(ticket);
	}

	/// <summary>
	/// Handle API Key requests
	/// </summary>
	/// <param name="key">The API key</param>
	/// <returns>The result of the authentication request</returns>
	public Task<AuthenticateResult> HandleApi(string key)
	{
		return Task.FromResult(
			AuthenticateResult.Fail(
				"API key authentication is not implemented yet. Please use JWT tokens instead."));
	}

	/// <summary>
	/// Cleans all of the <see cref="Prefixes"/> from the token
	/// </summary>
	/// <param name="token">The token to clean the prefixes from</param>
	/// <returns>The cleaned token</returns>
	public string CleanPrefixes(string token)
	{
		foreach (var prefix in Prefixes)
			if (token.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				return token[prefix.Length..].Trim();

		return token;
	}

	/// <summary>
	/// Scans all of the <see cref="ClientHeaders"/> and <see cref="ApiHeaders"/> for the token
	/// </summary>
	/// <returns>The token or null if none was found, and whether or not it's a client's key or an api key</returns>
	public (string? token, bool client) GetKey()
	{
		string? check(IEnumerable<KeyValuePair<string, StringValues>> keys, string[] headers)
		{
			foreach (var header in keys)
				if (headers.Contains(header.Key, StringComparer.InvariantCultureIgnoreCase))
					return CleanPrefixes(header.Value.ToString());

			return null;
		}

		var token = check(Request.Headers, ClientHeaders) ?? check(Request.Query, ClientHeaders);
		if (!string.IsNullOrEmpty(token))
			return (token, true);

		token = check(Request.Headers, ApiHeaders) ?? check(Request.Query, ApiHeaders);
		return (token, false);
	}
}

