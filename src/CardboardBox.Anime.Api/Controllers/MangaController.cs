using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Manga;

	[ApiController]
	public class MangaController : ControllerBase
	{
		private readonly IMangaService _manga;

		public MangaController(IMangaService manga)
		{
			_manga = manga;
		}

		[HttpGet, Route("manga")]
		[ProducesDefaultResponseType(typeof(Manga))]
		[ProducesResponseType(404), ProducesResponseType(400)]
		public async Task<IActionResult> Get([FromQuery] string url)
		{
			var (src, id) = _manga.DetermineSource(url);
			if (src == null || id == null) return BadRequest();

			var manga = await src.Manga(id);
			if (manga == null) return NotFound();

			return Ok(manga);
		}

		[HttpGet, Route("manga/chapter")]
		[ProducesDefaultResponseType(typeof(MangaChapterPages))]
		[ProducesResponseType(404), ProducesResponseType(400)]
		public async Task<IActionResult> Get([FromQuery] string url, [FromQuery] string chapter)
		{
			var (src, id) = _manga.DetermineSource(url);
			if (src == null || id == null) return BadRequest();

			var chap = await src.ChapterPages(id, chapter);
			if (chap == null) return NotFound();

			return Ok(chap);
		}
	}
}
