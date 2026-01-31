using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers;

using Auth;
using CardboardBox.Anime.Auth.Jwt;
using Database;
using System.Security.Claims;

[ApiController]
public class AuthController(
	IOAuthService _auth,
	IJwtTokenService _token,
	IDbService _db) : ControllerBase
{

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
		var token = _token.Empty()
			.Add(ClaimTypes.NameIdentifier, res.User.Id)
			.Add(ClaimTypes.Name, res.User.Nickname)
			.Add(ClaimTypes.Email, res.User.Email)
			.Add(ClaimTypes.UserData, res.User.Avatar)
			.Add(ClaimTypes.PrimarySid, res.Provider)
			.Add(ClaimTypes.PrimaryGroupSid, res.User.ProviderId);
		token.AddRange(user.Roles.Select(t => new Claim(ClaimTypes.Role, t)));

		var jwt = await _token.GenerateToken(token);

		return Ok(new AuthUserResponse
		{
			User = user,
			Id = profile.Id,
			Token = jwt,
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
