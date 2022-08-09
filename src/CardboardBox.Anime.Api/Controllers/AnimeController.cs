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
		private readonly IDbService _sql;

		public AnimeController(IAnimeMongoService db, IDbService sql)
		{
			_db = db;
			_sql = sql;
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
			var (total, anime) = await _sql.Anime.Search(search);
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

		[HttpGet, Route("anime/v2/filters")]
		public async Task<IActionResult> FiltersV2()
		{
			var filters = await _sql.Anime.Filters();
			return Ok(filters);
		}
	}
}
