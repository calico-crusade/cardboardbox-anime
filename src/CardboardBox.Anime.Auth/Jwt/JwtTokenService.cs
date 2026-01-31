using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace CardboardBox.Anime.Auth.Jwt;

/// <summary>
/// A service for interacting with JWT tokens
/// </summary>
public interface IJwtTokenService
{
	/// <summary>
	/// Parses the given token and returns a JwtToken object
	/// </summary>
	/// <param name="token">The token to parse</param>
	/// <param name="cancel">The cancellation token for the request</param>
	/// <returns>The JWT token</returns>
	Task<JwtToken?> ParseToken(string token, CancellationToken cancel = default);

	/// <summary>
	/// Generates a JWT token from the given JwtToken object
	/// </summary>
	/// <param name="token">The token to generate</param>
	/// <param name="cancel">The cancellation token for the request</param>
	/// <returns>The JWT token</returns>
	Task<string> GenerateToken(JwtToken token, CancellationToken cancel = default);

	/// <summary>
	/// Gets an empty JWT token with default values
	/// </summary>
	/// <returns>The empty JWT token</returns>
	JwtToken Empty();
}

internal class JwtTokenService(
	IConfiguration _config,
	IJwtKeyService _keys) : IJwtTokenService
{
	/// <summary>
	/// The issuer of the token
	/// </summary>
	public string Issuer => _config["OAuth:Issuer"] ?? "index-0.com";

	/// <summary>
	/// The audiences of the token
	/// </summary>
	public string Audience => _config["OAuth:Issuer"] ?? "index-0.com";

	/// <summary>
	/// How long the token should be valid for, in minutes
	/// </summary>
	public double ExpireMinutes => double.TryParse(_config["OAuth:JwtExpiry"], out var value) ? value : 7 * 24 * 60;

	/// <summary>
	/// How long the token should be valid for
	/// </summary>
	public TimeSpan Expires => TimeSpan.FromMinutes(ExpireMinutes);

	/// <summary>
	/// Reads all of the claims from the given token string
	/// </summary>
	/// <param name="token">The un-encrypted token</param>
	/// <returns>The JWT token</returns>
	public JwtToken? ReadToken(string token)
	{
		var handler = new JwtSecurityTokenHandler();
		var jwt = handler.ReadJwtToken(token);
		var output = new JwtToken();
		output.AddRange(jwt.Claims);
		output.Issuer = jwt?.Issuer;
		output.Audiences = jwt?.Audiences.ToArray() ?? [];
		output.Expiry = jwt?.ValidTo - DateTime.UtcNow;

		// Validate token
		if (jwt is null ||
			jwt.ValidTo < DateTime.UtcNow ||
			jwt.Issuer != Issuer ||
			!jwt.Audiences.Contains(Audience))
			return null;

		return output;
	}

	/// <summary>
	/// Writes the given token to an un-encrypted JWT string
	/// </summary>
	/// <param name="token">The token to write</param>
	/// <returns>The token string</returns>
	public string WriteToken(JwtToken token)
	{
		//Ensure a new JTI every time
		token.Set(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());

		var handler = new JwtSecurityTokenHandler();
		var securityToken = new JwtSecurityToken(
			issuer: Issuer,
			audience: Audience,
			claims: token,
			expires: DateTime.UtcNow.Add(Expires));
		return handler.WriteToken(securityToken);
	}

	/// <inheritdoc />
	public async Task<JwtToken?> ParseToken(string token, CancellationToken cancel)
	{
		var unencrypted = await _keys.Decrypt(token, cancel);
		if (string.IsNullOrWhiteSpace(unencrypted))
			return null;
		return ReadToken(unencrypted);
	}

	/// <inheritdoc />
	public async Task<string> GenerateToken(JwtToken token, CancellationToken cancel)
	{
		var unencrypted = WriteToken(token);
		return await _keys.Encrypt(unencrypted, cancel);
	}

	/// <inheritdoc />
	public JwtToken Empty() => new()
	{
		Issuer = Issuer,
		Audiences = [Audience],
		Expiry = Expires
	};
}

