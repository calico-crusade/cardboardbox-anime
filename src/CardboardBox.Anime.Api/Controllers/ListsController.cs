using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Auth;
	using Database;

	[ApiController, Authorize]
	public class ListsController : ControllerBase
	{
		private readonly IDbService _db;

		public ListsController(IDbService db)
		{
			_db = db;
		}

		[HttpGet, Route("lists")]
		public async Task<IActionResult> Get()
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var lists = await _db.Lists.ByProfile(user.Id);
			return Ok(lists);
		}

		[HttpPost, Route("lists")]
		public async Task<IActionResult> Post([FromBody] ListPost list)
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var profile = await _db.Profiles.Fetch(user.Id);
			if (profile == null) return Unauthorized();

			var id = await _db.Lists.Upsert(new DbList
			{
				ProfileId = profile.Id,
				Title = list.Title,
				Description = list.Description,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now,
			});

			return Ok(new { id });
		}

		[HttpPut, Route("lists")]
		public async Task<IActionResult> Put([FromBody] ListPut list)
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var profile = await _db.Profiles.Fetch(user.Id);
			if (profile == null) return Unauthorized();

			var current = await _db.Lists.Fetch(list.Id);
			if (current == null) return NotFound();
			if (current.ProfileId != profile.Id) return Unauthorized();

			current.Title = list.Title;
			current.Description = list.Description;
			current.UpdatedAt = DateTime.Now;
			await _db.Lists.Update(current);

			return Ok();
		}

		[HttpDelete, Route("lists/{id}")]
		public async Task<IActionResult> Delete(long id)
		{
			var user = this.UserFromIdentity();
			if (user == null) return Unauthorized();

			var profile = await _db.Profiles.Fetch(user.Id);
			if (profile == null) return Unauthorized();

			var current = await _db.Lists.Fetch(id);
			if (current == null) return NotFound();
			if (current.ProfileId != profile.Id) return Unauthorized();

			current.DeletedAt = DateTime.Now;
			await _db.Lists.Update(current);
			return Ok();
		}
	}

	public record class ListPost(string Title, string Description);
	
	public record class ListPut(string Title, string Description, long Id);
}
