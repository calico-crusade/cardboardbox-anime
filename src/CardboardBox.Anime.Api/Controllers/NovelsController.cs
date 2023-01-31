using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers;

using LightNovel.Core;
using LightNovel.Core.Database;

[ApiController]
public class NovelsController : ControllerBase
{
	private readonly ILnDbService _db;
	private readonly INovelApiService _api;
	private readonly INovelEpubService _epub;

	public NovelsController(
		ILnDbService db,
		INovelApiService api,
		INovelEpubService epub)
	{
		_db = db;
		_api = api;
		_epub = epub;
	}

	[HttpGet, Route("novels")]
	[ProducesDefaultResponseType(typeof(PaginatedResult<Series>))]
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

	[HttpGet, Route("novel/{bookId}/chapter/{chapterId}")]
	[ProducesDefaultResponseType(typeof(ChapterPages[])), ProducesResponseType(404)]
	public async Task<IActionResult> Chapter([FromRoute] long chapterId)
	{
		var chapters = await _db.ChapterPages.Chapter(chapterId);
		if (chapters == null || chapters.Length <= 0) return NotFound();
		return Ok(chapters);
	}

	[HttpGet, Route("novels/{seriesId}/pages")]
	[ProducesDefaultResponseType(typeof(PaginatedResult<Page>))]
	public async Task<IActionResult> Pages([FromRoute] long seriesId, [FromQuery] int page = 1, [FromQuery] int size = 100)
	{
		var data = await _db.Pages.Paginate(seriesId, page, size);
		return Ok(data);
	}

	[HttpGet, Route("novels/load"), ProducesDefaultResponseType(typeof(LoadResponse))]
	[ProducesResponseType(typeof(ErrorResponse), 400), ProducesResponseType(typeof(ErrorResponse), 404), ProducesResponseType(401)]
	public async Task<IActionResult> Load([FromQuery] string? url = null, [FromQuery] long? seriesId = null)
	{
		if (string.IsNullOrEmpty(url) && seriesId == null) return BadRequest(new ErrorResponse("Please specify the url or series ID"));
		if (!string.IsNullOrEmpty(url) && seriesId != null) return BadRequest(new ErrorResponse("Please specify only the url or only series ID, not both!"));

		if (seriesId != null)
		{
			var series = await _db.Series.Fetch(seriesId ?? 0);
			if (series == null) return NotFound(new ErrorResponse("Couldn't find series with that id!"));

			var count = await _api.Load(series);
			if (count == -1) return NotFound(new ErrorResponse("Couldn't find series with that id!"));
			if (count == 0) return NotFound(new ErrorResponse("No new chapters found for that series!"));

			return Ok(new LoadResponse(count, true));
		}

		var (newCount, isNew) = await _api.Load(url ?? "");
		if (newCount == -1) return NotFound(new ErrorResponse("Unable to load chapters from the given url!"));

		return Ok(new LoadResponse(newCount, isNew));
	}

	[HttpGet, Route("novels/{seriesId}/epubs")]
	public async Task<IActionResult> Epubs([FromRoute] long seriesId)
	{
		var books = await _db.Books.BySeries(seriesId);
		if (books.Length == 0) return NotFound();

		var ids = books.Select(t => t.Id).ToArray();
		var res = await _epub.Generate(ids);

		if (res == null) return StatusCode(500);
		var (stream, name, type) = res;

		return File(stream, type, name);
	}

	[HttpGet, Route("novels/{bookId}/epub")]
	public async Task<IActionResult> Epub([FromRoute] long bookId)
	{
		var res = await _epub.Generate(bookId);
		if (res == null) return StatusCode(500);
		var (stream, name, type) = res;
		return File(stream, type, name);
	}

	public record class ErrorResponse(string Message);
	public record class LoadResponse(int Count, bool New);
}
