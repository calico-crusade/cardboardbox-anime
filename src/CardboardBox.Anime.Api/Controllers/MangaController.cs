using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Manga;
	using Database;

	[ApiController]
	public class MangaController : ControllerBase
	{
		private readonly IMangaService _manga;

		public MangaController(IMangaService manga)
		{
			_manga = manga;
		}

		[HttpGet, Route("manga")]
		[ProducesDefaultResponseType(typeof(PaginatedResult<DbManga>))]
		public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int size = 100)
		{
			var data = await _manga.Manga(page, size);
			return Ok(data);
		}

		[HttpGet, Route("manga/{id}")]
		[ProducesDefaultResponseType(typeof(MangaWithChapters)), ProducesResponseType(404)]
		public async Task<IActionResult> Get([FromRoute] long id)
		{
			var manga = await _manga.Manga(id);
			if (manga == null) return NotFound();
			return Ok(manga);
		}

		[HttpGet, Route("manga/{id}/{chapterId}/pages")]
		[ProducesDefaultResponseType(typeof(string[])), ProducesResponseType(404)]
		public async Task<IActionResult> GetPages([FromRoute] long chapterId)
		{
			var manga = await _manga.MangaPages(chapterId);
			if (manga == null || manga.Length == 0) return NotFound();

			return Ok(manga);
		}

		[HttpGet, Route("manga/load")]
		[ProducesDefaultResponseType(typeof(MangaWithChapters))]
		[ProducesResponseType(404), ProducesResponseType(400)]
		public async Task<IActionResult> Load([FromQuery] string url, [FromQuery] bool force = false)
		{
			var manga = await _manga.Manga(url, force);
			if (manga == null) return NotFound();

			return Ok(manga);
		}
	}
}
