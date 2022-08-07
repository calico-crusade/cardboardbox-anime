using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Core;
	using Core.Models;
	using Database;

	[ApiController]
	public class AnimeController : ControllerBase
	{
		private readonly IAnimeMongoService _db;
		private readonly IAnimeDbService _sql;

		public AnimeController(IAnimeMongoService db, IAnimeDbService sql)
		{
			_db = db;
			_sql = sql;
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

		[HttpPost, Route("anime/v2"), ProducesDefaultResponseType(typeof(PaginatedResult<DbAnime>))]
		public async Task<IActionResult> AllV2([FromBody] FilterSearch search)
		{
			var (total, anime) = await _sql.Search(search);
			return Ok(new
			{
				pages = Math.Ceiling((double)total / search.Size),
				count = total,
				results = anime
			});
		}

		[HttpGet, Route("anime/filters")]
		public async Task<IActionResult> Filters()
		{
			var filters = await _db.Filters();
			return Ok(filters);
		}
	}
}
