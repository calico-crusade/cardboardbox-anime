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

		[HttpPost, Route("list-map")]
		public async Task<IActionResult> Post([FromBody] ListMapPost map)
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var profile = await _db.Profiles.Fetch(user.Id);
			if (profile == null) return Unauthorized();

			var list = await _db.Lists.Fetch(map.ListId);
			if (list == null) return NotFound();
			if (list.ProfileId != profile.Id) return Unauthorized();

			await _db.ListMaps.Upsert(new DbListMap
			{
				AnimeId = map.AnimeId,
				ListId = map.ListId,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now
			});

			return Ok();
		}

		[HttpDelete, Route("list-map/{listId}/{animeId}")]
		public async Task<IActionResult> Delete(long listId, long animeId)
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var profile = await _db.Profiles.Fetch(user.Id);
			if (profile == null) return Unauthorized();

			var list = await _db.Lists.Fetch(listId);
			if (list == null) return NotFound();
			if (list.ProfileId != profile.Id) return Unauthorized();

			await _db.ListMaps.Upsert(new DbListMap
			{
				AnimeId = animeId,
				ListId = listId,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now,
				DeletedAt = DateTime.Now
			});

			return Ok();
		}
	}

	public record class ListMapPost(long AnimeId, long ListId);
}
