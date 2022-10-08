using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Database;
	using LightNovel.Core;

	[ApiController]
	public class NovelsController : ControllerBase
	{
		private readonly ILnDbService _db;
		private readonly ILightNovelApiService _api;

		public NovelsController(
			ILnDbService db, 
			ILightNovelApiService api)
		{
			_db = db;
			_api = api;
		}

		[HttpGet, Route("novels"), ProducesDefaultResponseType(typeof(PaginatedResult<Series>))]
		public async Task<IActionResult> Series([FromQuery] int page = 1, [FromQuery] int size = 100)
		{
			var data = await _db.Series.Paginate(page, size);
			return Ok(data);
		}

		[HttpGet, Route("novels/{seriesId}")]
		[ProducesDefaultResponseType(typeof(PartialScaffold)), ProducesResponseType(404)]
		public async Task<IActionResult> Series([FromRoute] long seriesId)
		{
			var data = await _db.Series.PartialScaffold(seriesId);
			if (data == null) return NotFound();
			return Ok(data);
		}

		[HttpGet, Route("novel/{seriesId}/books")]
		[ProducesDefaultResponseType(typeof(Book[])), ProducesResponseType(404)]
		public async Task<IActionResult> Books([FromRoute] long seriesId)
		{
			var books = await _db.Books.BySeries(seriesId);
			if (books.Length == 0) return NotFound();
			return Ok(books);
		}

		[HttpGet, Route("novel/{bookId}/chapters")]
		[ProducesDefaultResponseType(typeof(Chapter[])), ProducesResponseType(404)]
		public async Task<IActionResult> Chapters([FromRoute] long bookId)
		{
			var books = await _db.Chapters.ByBook(bookId);
			if (books.Length == 0) return NotFound();
			return Ok(books);
		}

		[HttpGet, Route("novels/{seriesId}/pages"), ProducesDefaultResponseType(typeof(PaginatedResult<Page>))]
		public async Task<IActionResult> Pages([FromRoute] long seriesId, [FromQuery] int page = 1, [FromQuery] int size = 100)
		{
			var data = await _db.Pages.Paginate(seriesId, page, size);
			return Ok(data);
		}
	}
}
