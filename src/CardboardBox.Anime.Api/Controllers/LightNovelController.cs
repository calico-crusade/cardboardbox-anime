using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Database;
	using ICSharpCode.SharpZipLib.Core;
	using LightNovel.Core;

	[ApiController]
	//[Authorize]
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

		[HttpPost, Route("ln/{bookId}/epub"), DisableRequestSizeLimit]
		public async Task<IActionResult> Epub([FromRoute] string bookId, [FromBody] EpubSettings[] settings)
		{
			var ids = await _api.GenerateEpubs(bookId, settings);

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

			ms.Position = 0;

			return File(ms, "application/zip", "epubs.zip");
		}
	}
}
