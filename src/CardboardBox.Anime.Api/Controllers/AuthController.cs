using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
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

			var token = _token.GenerateToken(res);
			var profile = new DbProfile
			{
				Avatar = res.User.Avatar,
				Email = res.User.Email,
				PlatformId = res.User.Id,
				Username = res.User.Nickname,
			};
			var id = await _db.Profiles.Upsert(profile);

			return Ok(new
			{
				user = res.User,
				token,
				id
			});
		}

		[HttpGet, Route("auth"), Authorize]
		public IActionResult Me()
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			return Ok(user);
		}
	}
}
