using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Database;
	using LightNovel.Core;

	[ApiController]
	public class LightNovelController : ControllerBase
	{
		private readonly IChapterDbService _ln;
		private readonly ILightNovelApiService _api;

		public LightNovelController(
			IChapterDbService ln, 
			ILightNovelApiService api)
		{
			_ln = ln;
			_api = api;
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

		[HttpGet, Route("ln/{bookId}/chapters")]
		[ProducesDefaultResponseType(typeof(ChaptersResponse))]
		[ProducesResponseType(404)]
		public async Task<IActionResult> Chapters([FromRoute] string bookId)
		{
			var (book, chaps) = await _ln.ChapterList(bookId);
			if (book == null || chaps == null || chaps.Length == 0) return NotFound();
			return Ok(new ChaptersResponse(book, chaps));
		}

		[HttpPost, Route("ln/{bookId}/epub"), DisableRequestSizeLimit]
		public async Task<IActionResult> Epub([FromRoute] string bookId, [FromBody] EpubSettings[] settings)
		{
			var (ids, dir) = await _api.GenerateEpubs(bookId, settings);

			if (ids.Length == 0) return NoContent();

			var ms = new MemoryStream();
			using (var o = new ZipOutputStream(ms))
			{
				o.IsStreamOwner = false;
				foreach (var id in ids)
				{
					await o.PutNextEntryAsync(new ZipEntry(Path.GetFileName(id)));
					using (var io = System.IO.File.OpenRead(id))
						StreamUtils.Copy(io, o, new byte[4096]);
					await o.CloseEntryAsync(CancellationToken.None);
				}
			}

			new DirectoryInfo(dir).Delete(true);

			ms.Position = 0;

			return File(ms, "application/zip", "epubs.zip");
		}

		[HttpGet, Route("ln/load/{bookId}"), ProducesDefaultResponseType(typeof(ChapterLoadResponse))]
		public async Task<IActionResult> RefreshBook([FromRoute] string bookId)
		{
			var (id, count) = await _api.LoadFromBookId(bookId);
			if (count == -1) return BadRequest(new ChapterLoadResponse(null, false, 0, "Could not find the correct source!"));
			if (count <= 1) return NotFound(new ChapterLoadResponse(id, false, count, "No new chapters detected!"));

			return Ok(new ChapterLoadResponse(id, false, count));
		}

		[HttpPost, Route("ln/load"), ProducesDefaultResponseType(typeof(ChapterLoadResponse))]
		public async Task<IActionResult> LoadFromUrl([FromBody] string url)
		{
			var (id, isNew, count) = await _api.LoadFromUrl(url);
			if (count == -1) return BadRequest(new ChapterLoadResponse(null, false, 0, "Could not find the correct source!"));
			if (count == 0 && isNew) return StatusCode(500, new ChapterLoadResponse(id, isNew, count, "Attempted to load new book, but couldn't find any chapters. Do you have the right source?"));
			if (count <= 1 && !isNew) return NotFound(new ChapterLoadResponse(id, isNew, count, "No new chapters detected!"));

			return Ok(new ChapterLoadResponse(id, isNew, count));
		}

		[HttpGet, Route("ln/refresh"), ProducesDefaultResponseType(typeof(RefreshResponse[]))]
		public async Task<IActionResult> Refresh()
		{
			var (_, books) = await _ln.Books();
			var resp = await Task.WhenAll(books
				.Select(async c =>
				{
					var (id, count) = await _api.LoadFromBookId(c.Id);
					return new RefreshResponse(c, new ChapterLoadResponse(c.Id, false, count));
				}));

			return Ok(resp);
		}
	}

	public record class ChapterLoadResponse(string? BookId, bool IsNew, int? Count, string? Message = null);
	public record class RefreshResponse(DbBook Book, ChapterLoadResponse Response);
	public record class ChaptersResponse(DbBook Book, DbChapterLimited[] Chapters);
}
