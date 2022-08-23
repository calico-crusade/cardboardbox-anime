using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Auth;
	using Core;
	using Core.Models;
	using Database;

	using Funimation;
	using HiDive;
	using Vrv;

	[ApiController]
	public class AnimeController : ControllerBase
	{
		private readonly IDbService _sql;
		private readonly IFunimationApiService _fun;
		private readonly IHiDiveApiService _hidive;
		private readonly IVrvApiService _vrv;

		public AnimeController(IDbService sql, IFunimationApiService fun, IHiDiveApiService hidive, IVrvApiService vrv)
		{
			_sql = sql;
			_fun = fun;
			_hidive = hidive;
			_vrv = vrv;
		}

		private IAnimeApiService[]? ResolveService(string platform)
		{
			return platform.ToLower().Trim() switch
			{
				"funimation" => new[] { _fun },
				"hidive" => new[] { _hidive },
				"vrv" => new[] { _vrv },
				"all" => new IAnimeApiService[] { _fun, _hidive, _vrv },
				_ => Array.Empty<IAnimeApiService>(),
			};
		}

		[HttpPost, Route("anime"), ProducesDefaultResponseType(typeof(PaginatedResult<DbAnime>))]
		public async Task<IActionResult> AllV2([FromBody] FilterSearch search)
		{
			var user = this.UserFromIdentity();
			var (total, anime) = await _sql.Anime.Search(search, user?.Id);
			return Ok(new
			{
				pages = Math.Ceiling((double)total / search.Size),
				count = total,
				results = anime
			});
		}

		[HttpPost, Route("anime/random"), ProducesDefaultResponseType(typeof(DbAnime[]))]
		public async Task<IActionResult> Random([FromBody] FilterSearch search)
		{
			search.Size = 1;
			search.Page = 1;
			var user = this.UserFromIdentity();
			var results = await _sql.Anime.Random(search, user?.Id) ?? Array.Empty<DbAnime>();
			return Ok(results);
		}

		[HttpGet, Route("anime/filters")]
		public async Task<IActionResult> FiltersV2()
		{
			var filters = await _sql.Anime.Filters();
			return Ok(filters);
		}

		[HttpGet, Route("anime/load/{platform}")]
		public async Task<IActionResult> Load([FromRoute] string platform)
		{
			var service = ResolveService(platform);
			if (service == null || service.Length == 0) return NotFound();

			await Task.WhenAll(service.Select(async t =>
			{
				var data = t.All();
				await foreach (var item in data)
					await _sql.Anime.Upsert(item.Clean());
			}));
			
			return Ok();
		}

		[HttpPost, Route("anime/load/{platform}")]
		public async Task<IActionResult> Load([FromRoute] string platform, [FromBody] VrvLoadRequest vrv)
		{
			var service = ResolveService(platform);
			if (service == null || service.Length == 0) return NotFound();

			await Task.WhenAll(service.Select(async t =>
			{
				var data = t is IVrvApiService v ? v.All(vrv) : t.All();
				await foreach (var item in data)
					await _sql.Anime.Upsert(item.Clean());
			}));

			return Ok();
		}
	}
}
