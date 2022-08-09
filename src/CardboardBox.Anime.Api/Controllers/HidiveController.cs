using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Database;
	using HiDive;

	[ApiController]
	public class HidiveController : ControllerBase
	{
		private readonly IDbService _db;
		private readonly IHiDiveApiService _hidive;

		public HidiveController(IDbService db, IHiDiveApiService hidive)
		{
			_db = db;
			_hidive = hidive;
		}

		[HttpGet, Route("hidive/load")]
		public async Task<IActionResult> Load()
		{
			var data = _hidive.All();

			await foreach (var item in data)
				await _db.Anime.Upsert(item.Clean());

			return Ok();
		}

		[HttpGet, Route("hidive")]
		public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int size = 100, [FromQuery] bool asc = true)
		{
			var data = await _db.Anime.Search(new()
			{
				Page = page,
				Size = size,
				Ascending = asc,
				Queryables = new()
				{
					Platforms = new[] { "hidive" }
				}
			});

			return Ok(data);
		}
	}
}
