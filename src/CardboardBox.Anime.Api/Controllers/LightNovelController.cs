using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Database;

	[ApiController, Authorize]
	public class LightNovelController : ControllerBase
	{
		private readonly IChapterDbService _ln;

		public LightNovelController(IChapterDbService ln)
		{
			_ln = ln;
		}

		[HttpGet, Route("ln"), ProducesDefaultResponseType(typeof(PaginatedResult<DbBook>))]
		public async Task<IActionResult> LightNovels([FromQuery] int page = 1, [FromQuery] int size = 100)
		{
			var (total, chaps) = await _ln.Books(page, size);
			return Ok(new
			{
				pages = Math.Ceiling((double)total / size),
				count = total,
				results = chaps
			});
		}

		[HttpGet, Route("ln/{bookId}"), ProducesDefaultResponseType(typeof(PaginatedResult<DbChapter>))]
		public async Task<IActionResult> LightNovel([FromRoute]string bookId, [FromQuery] int page = 1, [FromQuery] int size = 10)
		{
			var (total, chaps) = await _ln.Chapters(bookId, page, size);
			return Ok(new
			{
				pages = Math.Ceiling((double)total / size),
				count = total,
				results = chaps
			});
		}
	}
}
