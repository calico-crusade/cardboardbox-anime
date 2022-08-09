using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Auth;
	using Database;

	[ApiController, Authorize]
	public class ListMapController : ControllerBase
	{
		private readonly IDbService _db;

		public ListMapController(IDbService db)
		{
			_db = db;
		}

		[HttpGet, Route("list-map")]
		public async Task<IActionResult> Get()
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var data = await _db.ListMaps.Get(user.Id);
			return Ok(data);
		}

		[HttpGet, Route("list-map/{listId}/{animeId}")]
		public async Task<IActionResult> Toggle(long listId, long animeId)
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var profile = await _db.Profiles.Fetch(user.Id);
			if (profile == null) return Unauthorized();

			var list = await _db.Lists.Fetch(listId);
			if (list == null) return NotFound();
			if (list.ProfileId != profile.Id) return Unauthorized();

			var active = await _db.ListMaps.Toggle(animeId, listId);
			return Ok(new
			{
				inList = active
			});
		}
	}

	public record class ListMapPost(long AnimeId, long ListId);
}
