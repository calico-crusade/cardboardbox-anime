using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CardboardBox.Anime.Api.Controllers;

using Auth;
using Database;

[ApiController]
public class AuthController : ControllerBase
{
	private readonly IOAuthService _auth;
	private readonly ITokenService _token;
	private readonly IDbService _db;

	public AuthController(
		IOAuthService auth, 
		ITokenService token,
		IDbService db)
	{
		_auth = auth;
		_token = token;
		_db = db;
	}

	[HttpGet, Route("auth/{code}")]
	public async Task<IActionResult> Auth(string code)
	{
		var res = await _auth.ResolveCode(code);
		if (res == null || !string.IsNullOrEmpty(res.Error))
			return Unauthorized(new
			{
				error = res?.Error ?? "Login Failed"
			});

		var profile = new DbProfile
		{
			Avatar = res.User.Avatar,
			Email = res.User.Email,
			PlatformId = res.User.Id,
			Username = res.User.Nickname,
			Provider = res.User.Provider,
			ProviderId = res.User.ProviderId,
		};
		var id = await _db.Profiles.Upsert(profile);

		profile = await _db.Profiles.Fetch(res.User.Id);

		var roles = profile.Admin ? new[] { "Admin" } : Array.Empty<string>();
		var token = _token.GenerateToken(res, roles);
		

		return Ok(new
		{
			user = new
			{
				roles,
				nickname = res.User.Nickname,
				avatar = res.User.Avatar,
				id = res.User.Id,
				email = res.User.Email
			},
			token,
			id
		});
	}

	[HttpGet, Route("auth"), Authorize]
	public IActionResult Me()
	{
		var user = this.UserFromIdentity();
		if (user == null) return Unauthorized();

		var roles = User.Claims.Where(t => t.Type == ClaimTypes.Role).Select(t => t.Value).ToArray();

		return Ok(new
		{
			roles,
			nickname = user.Nickname,
			avatar = user.Avatar,
			id = user.Id,
			email = user.Email
		});
	}
}
