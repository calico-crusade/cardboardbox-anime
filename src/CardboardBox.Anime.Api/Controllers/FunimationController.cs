using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Database;
	using Funimation;

	[ApiController]
	public class FunimationController : ControllerBase
	{
		private readonly IDbService _db;
		private readonly IFunimationApiService _fun;

		public FunimationController(IDbService db, IFunimationApiService fun)
		{
			_db = db;
			_fun = fun;
		}

		[HttpGet, Route("funimation/load")]
		public async Task<IActionResult> Load()
		{
			var data = _fun.All();

			await foreach(var item in data)
				await _db.Anime.Upsert(item.Clean());

			return Ok();
		}

		[HttpGet, Route("funimation")]
		public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int size = 100, [FromQuery] bool asc = true)
		{
			var data = await _db.Anime.Search(new()
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
