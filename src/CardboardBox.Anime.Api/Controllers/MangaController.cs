using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Auth;
	using Core;
	using Core.Models;
	using Manga;
	using Database;

	[ApiController]
	public class MangaController : ControllerBase
	{
		private readonly IMangaService _manga;
		private readonly IDbService _db;
		private readonly IMangaEpubService _epub;

		public MangaController(
			IMangaService manga,
			IDbService db,
			IMangaEpubService epub)
		{
			_manga = manga;
			_db = db;
			_epub = epub;
		}

		[HttpGet, Route("manga")]
		[ProducesDefaultResponseType(typeof(PaginatedResult<DbManga>))]
		public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int size = 100)
		{
			var data = await _manga.Manga(page, size);
			return Ok(data);
		}

		[HttpGet, Route("manga/filters")]
		[ProducesDefaultResponseType(typeof(Filter[]))]
		public async Task<IActionResult> Get()
		{
			var data = await _db.Manga.Filters();
			return Ok(data);
		}

		[HttpGet, Route("manga/in-progress"), Authorize]
		[ProducesDefaultResponseType(typeof(MangaProgress[]))]
		public async Task<IActionResult> InProgress()
		{
			var id = this.UserFromIdentity()?.Id;
			if (string.IsNullOrEmpty(id)) return BadRequest();
			var data = await _db.Manga.InProgress(id);
			return Ok(data);
		}

		[HttpGet, Route("manga/{id}")]
		[ProducesDefaultResponseType(typeof(MangaWithChapters)), ProducesResponseType(404)]
		public async Task<IActionResult> Get([FromRoute] long id)
		{
			var manga = await _manga.Manga(id, this.UserFromIdentity()?.Id);
			if (manga == null) return NotFound();
			return Ok(manga);
		}

		[HttpGet, Route("manga/{id}/progress"), Authorize]
		[ProducesDefaultResponseType(typeof(DbMangaProgress)), ProducesResponseType(404)]
		public async Task<IActionResult> GetProgress([FromRoute] long id)
		{
			var platform = this.UserFromIdentity()?.Id;
			if (string.IsNullOrEmpty(platform)) return BadRequest();

			var progress = await _db.Manga.GetProgress(platform, id);
			if (progress == null) return NotFound();

			return Ok(progress);
		}

		[HttpGet, Route("manga/{id}/{chapterId}/pages")]
		[ProducesDefaultResponseType(typeof(string[])), ProducesResponseType(404)]
		public async Task<IActionResult> GetPages([FromRoute] long chapterId, [FromQuery] bool refetch = false)
		{
			var manga = await _manga.MangaPages(chapterId, refetch);
			if (manga == null || manga.Length == 0) return NotFound();

			return Ok(manga);
		}

		[HttpPost, Route("manga/{id}/{chapterId}/bookmark"), Authorize]
		public async Task<IActionResult> Bookmark([FromRoute] long id, [FromRoute] long chapterId, [FromBody] int[] pages)
		{
			var pid = this.UserFromIdentity()?.Id;
			if (string.IsNullOrEmpty(pid)) return Unauthorized();

			await _db.Manga.Bookmark(id, chapterId, pages, pid);
			return Ok();
		}

		[HttpGet, Route("manga/load")]
		[ProducesDefaultResponseType(typeof(MangaWithChapters))]
		[ProducesResponseType(404), ProducesResponseType(400)]
		public async Task<IActionResult> Load([FromQuery] string url, [FromQuery] bool force = false)
		{
			var manga = await _manga.Manga(url, this.UserFromIdentity()?.Id, force);
			if (manga == null) return NotFound();

			return Ok(manga);
		}

		[HttpPost, Route("manga"), Authorize]
		public async Task<IActionResult> Post([FromBody] MangaProgressPost data)
		{
			var id = this.UserFromIdentity()?.Id;
			if (id == null) return BadRequest();

			var profile = await _db.Profiles.Fetch(id);
			if (profile == null) return Unauthorized();

			await _db.Manga.Upsert(new DbMangaProgress
			{
				ProfileId = profile.Id,
				MangaId = data.MangaId,
				MangaChapterId = data.MangaChapterId,
				PageIndex = data.Page
			});

			return Ok();
		}

		[HttpPost, Route("manga/search")]
		[ProducesDefaultResponseType(typeof(PaginatedResult<DbManga>))]
		public async Task<IActionResult> Search([FromBody] MangaFilter filter)
		{
			var data = await _db.Manga.Search(filter);
			return Ok(data);
		}

		[HttpGet, Route("manga/{id}/download")]
		public async Task<IActionResult> Download([FromRoute] long id)
		{
			var data = await _epub.Generate(id);
			if (data == null) return NotFound();

			return File(data.Stream, data.Mimetype, data.Name);
		}

		[HttpGet, Route("manga/{id}/favourite"), Authorize]
		[ProducesDefaultResponseType(typeof(bool))]
		public async Task<IActionResult> ToggleFavourite(long id)
		{
			var uid = this.UserFromIdentity()?.Id;
			if (string.IsNullOrEmpty(uid)) return Unauthorized();

			var res = await _db.Manga.Favourite(uid, id);
			if (res == null) return Unauthorized();

			return Ok(res);
		}

		[HttpGet, Route("manga/refresh")]
		[ProducesDefaultResponseType(typeof(MangaWorked[]))]
		public async Task<IActionResult> Refresh([FromQuery] int count = 5)
		{
			var data = await _manga.Updated(count, this.UserFromIdentity()?.Id);
			return Ok(data);
		}
	}
}
