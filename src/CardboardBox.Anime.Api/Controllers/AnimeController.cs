using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Core;
	using Core.Models;

	[ApiController]
	public class AnimeController : ControllerBase
	{
		private readonly IAnimeMongoService _db;

		public AnimeController(IAnimeMongoService db)
		{
			_db = db;
		}

		[HttpGet, Route("anime"), ProducesDefaultResponseType(typeof(PaginatedResult<Anime>))]
		public async Task<IActionResult> All([FromQuery]int page = 1, [FromQuery]int size = 100, [FromQuery]bool asc = true)
		{
			var data = await _db.All(page, size, asc);
			return Ok(data);
		}

		[HttpPost, Route("anime"), ProducesDefaultResponseType(typeof(PaginatedResult<Anime>))]
		public async Task<IActionResult> All([FromBody] FilterSearch search)
		{
			var data = await _db.All(search);
			return Ok(data);
		}

		[HttpGet, Route("anime/filters")]
		public async Task<IActionResult> Filters()
		{
			var filters = await _db.Filters();
			return Ok(filters);
		}
	}
}
