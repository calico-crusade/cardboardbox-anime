using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Core;
	using Core.Models;

	[ApiController]
	public class AnimeController : ControllerBase
	{
		private readonly IAnimeMongoService _mongo;

		public AnimeController(IAnimeMongoService mongo)
		{
			_mongo = mongo;
		}

		[HttpGet, Route("anime/all"), ProducesDefaultResponseType(typeof(PaginatedResult<Anime>))]
		public async Task<IActionResult> All([FromQuery]int page = 1, [FromQuery]int size = 100, [FromQuery]bool asc = true)
		{
			var data = await _mongo.All(page, size, asc);
			return Ok(data);
		}

		[HttpPost, Route("anime/all"), ProducesDefaultResponseType(typeof(PaginatedResult<Anime>))]
		public async Task<IActionResult> All([FromBody] FilterSearch search)
		{
			var data = await _mongo.All(search);
			return Ok(data);
		}

		[HttpGet, Route("anime/filters")]
		public async Task<IActionResult> Filters()
		{
			var filters = await _mongo.Filters();
			return Ok(filters);
		}
	}
}
