using System.Security.Claims;

namespace CardboardBox.Anime.Auth;

/// <summary>
/// Represents a JWT token
/// </summary>
public class JwtToken() : List<Claim>
{
	/// <summary>
	/// The issuer of the token
	/// </summary>
	public string? Issuer { get; set; }

	/// <summary>
	/// The audiences of the token
	/// </summary>
	public string[] Audiences { get; set; } = [];

	/// <summary>
	/// How long until the key expires
	/// </summary>
	public TimeSpan? Expiry { get; set; }

	/// <summary>
	/// Fetches all of the claims by their key
	/// </summary>
	/// <param name="key">The key for the claims</param>
	/// <returns>All of the claims</returns>
	public Claim[] this[string key]
	{
		get => this.Where(t => t.Type == key).ToArray();
		set
		{
			var claims = this.Where(t => t.Type == key).ToArray();
			foreach (var claim in claims)
				Remove(claim);

			AddRange(value);
		}
	}

	/// <summary>
	/// Adds or updates the given claim value
	/// </summary>
	/// <param name="key">The key of the claim</param>
	/// <param name="value">The value of the claim</param>
	/// <returns>The current token instance for fluent method chaining</returns>
	public JwtToken Set(string key, string value)
	{
		var claim = new Claim(key, value);
		this[key] = [claim];
		return this;
	}

	/// <summary>
	/// Adds the given claim to the token
	/// </summary>
	/// <param name="key">The key of the claim</param>
	/// <param name="value">The value of the claim</param>
	/// <returns>The current token instance for fluent method chaining</returns>
	public JwtToken Add(string key, string value)
	{
		Add(new Claim(key, value));
		return this;
	}
}
