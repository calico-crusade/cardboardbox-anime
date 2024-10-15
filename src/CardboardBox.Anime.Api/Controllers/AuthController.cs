using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
	[ProducesDefaultResponseType(typeof(AuthUserResponse))]
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

        var user = (AuthUserResponse.UserData)profile;
		var token = _token.GenerateToken(res, user.Roles);

		return Ok(new AuthUserResponse
		{
			User = user,
			Id = profile.Id,
			Token = token,
		});
	}

	[HttpGet, Route("auth"), Authorize]
	[ProducesDefaultResponseType(typeof(AuthUserResponse.UserData))]
	public async Task<IActionResult> Me()
	{
		var user = this.UserFromIdentity();
		if (user == null) return Unauthorized();

		var profile = await _db.Profiles.Fetch(user.Id);
		if (profile == null) return NotFound();

		var data = (AuthUserResponse.UserData)profile;
		return Ok(data);
	}

	[HttpPost, Route("auth/settings"), Authorize]
	public async Task<IActionResult> Settings([FromBody] SettingsRequest request)
	{
		var user = this.UserFromIdentity();
		if (user == null) return Unauthorized();

		if (string.IsNullOrEmpty(request.Settings)) request.Settings = "{}";

		await _db.Profiles.UpdateSettings(user.Id, request.Settings);
		return Ok();
	}
}
