using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Auth;

	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IOAuthService _auth;
		private readonly ITokenService _token;

		public AuthController(
			IOAuthService auth, 
			ITokenService token)
		{
			_auth = auth;
			_token = token;
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
			return Ok(new
			{
				user = res.User,
				token
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
