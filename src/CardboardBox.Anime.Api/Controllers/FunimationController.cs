using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Core;
	using Funimation;

	[ApiController]
	public class FunimationController : ControllerBase
	{
		private readonly IAnimeMongoService _db;
		private readonly IFunimationApiService _fun;

		public FunimationController(IAnimeMongoService db, IFunimationApiService fun)
		{
			_db = db;
			_fun = fun;
		}

		[HttpGet, Route("funimation/load")]
		public async Task<IActionResult> Load()
		{
			var data = await _fun.All().ToArrayAsync();

			if (data.Length == 0) return NotFound();

			await _db.Upsert(data.Clean());

			return Ok();
		}

		[HttpGet, Route("funimation")]
		public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int size = 100, [FromQuery] bool asc = true)
		{
			var data = await _db.All(new()
			{
				Page = page,
				Size = size,
				Ascending = asc,
				Queryables = new()
				{
					Platforms = new[] { "funimation" }
				}
			});

			return Ok(data);
		}
	}
}
