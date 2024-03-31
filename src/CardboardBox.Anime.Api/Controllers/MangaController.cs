using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace CardboardBox.Anime.Api.Controllers;

using Auth;
using Core;
using Core.Models;
using Database;
using Match;
using Match.SauceNao;
using Manga;

[ApiController]
public class MangaController : ControllerBase
{
	private readonly IMangaService _manga;
	private readonly IDbService _db;
	private readonly IMangaEpubService _epub;
	private readonly IMangaImageService _image;
	private readonly IMangaSearchService _search;
	private readonly IMangaMatchService _match;
	private readonly ISauceNaoApiService _sauce;

	public MangaController(
		IMangaService manga,
		IDbService db,
		IMangaEpubService epub,
		IMangaImageService image,
		IMangaSearchService search,
		ISauceNaoApiService sauce,
		IMangaMatchService match)
	{
		_manga = manga;
		_db = db;
		_epub = epub;
		_image = image;
		_search = search;
		_sauce = sauce;
		_match = match;
	}

	// GET manga
	[HttpGet, Route("manga")]
	[ProducesDefaultResponseType(typeof(PaginatedResult<DbManga>))]
	public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int size = 100)
	{
		var data = await _manga.Manga(page, size);
		return Ok(data);
	}

	// GET search/filters
	[HttpGet, Route("manga/filters")]
	[ProducesDefaultResponseType(typeof(Filter[]))]
	public async Task<IActionResult> Get()
	{
		var data = await _db.Manga.Filters();
		return Ok(data);
    }

    // GET manga/import
    [HttpGet, Route("manga/load")]
    [ProducesDefaultResponseType(typeof(MangaWithChapters))]
    [ProducesResponseType(404), ProducesResponseType(400)]
    public async Task<IActionResult> Load([FromQuery] string url, [FromQuery] bool force = false)
    {
        var manga = await _manga.Manga(url, this.UserFromIdentity()?.Id, force);
        if (manga == null) return NotFound();

        return Ok(manga);
    }

    // POST search
    [HttpPost, Route("manga/search-v2"), Route("manga/search")]
    [ProducesDefaultResponseType(typeof(PaginatedResult<MangaProgress>))]
    public async Task<IActionResult> SearchV2([FromBody] MangaFilter filter)
    {
        var data = await _db.Manga.Search(filter, this.UserFromIdentity()?.Id);
        return Ok(data);
    }

    // GET manga/random 
    [HttpGet, Route("manga/random/{count}")]
    [ProducesDefaultResponseType(typeof(DbManga[]))]
    public async Task<IActionResult> Random([FromRoute] int count)
    {
        var data = await _db.Manga.Random(count);
        return Ok(data);
    }

    // GET manga/random
    [HttpGet, Route("manga/random")]
    [ProducesDefaultResponseType(typeof(MangaWithChapters))]
    public async Task<IActionResult> Random()
    {
        var data = await _db.Manga.Random(this.UserFromIdentity()?.Id);
        return Ok(data);
    }

    // GET search/reverse
    [HttpGet, Route("manga/search"), Route("manga/image-search")]
    [ProducesDefaultResponseType(typeof(ImageSearchResults))]
    public async Task<IActionResult> Search([FromQuery] string path)
    {
        var lookup = await _search.Search(path);
        return Ok(lookup);
    }

    // GET search/reverse
    [HttpPost, Route("manga/image-search")]
    [ProducesDefaultResponseType(typeof(ImageSearchResults))]
    public async Task<IActionResult> Search(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        var lookup = await _search.Search(ms, file.FileName);
        return Ok(lookup);
    }

    // GET manga/providers
    [HttpGet, Route("manga/providers")]
    [ProducesDefaultResponseType(typeof(MangaProvider[]))]
    public IActionResult Providers()
    {
        return Ok(_manga.Providers);
    }

    // PUT manga
    [HttpPut, Route("manga/display-title"), AdminAuthorize]
    public async Task<IActionResult> SetDisplayTitle([FromBody] DisplayTitleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            req.Title = null;

        await _db.Manga.SetDisplayTitle(req.Id, req.Title);
        return Ok();
    }

    // PUT manga
    [HttpPut, Route("manga/ordinal-reset"), AdminAuthorize]
    public async Task<IActionResult> ResetOrdinal([FromBody] OrdinalResetRequest req)
    {
        await _db.Manga.SetOrdinalReset(req.Id, req.Reset);
        return Ok();
    }

    // GET chapter/{id}/pages
    [HttpGet, Route("manga/{id}/{chapterId}/pages")]
    [ProducesDefaultResponseType(typeof(string[])), ProducesResponseType(404)]
    public async Task<IActionResult> GetPages([FromRoute] long chapterId, [FromQuery] bool refetch = false)
    {
        var manga = await _manga.MangaPages(chapterId, refetch);
        return Ok(manga ?? Array.Empty<string>());
    }

    // GET chapter/{id}/reset
    [HttpGet, Route("manga/{id}/reset/{chapterId}")]
    public async Task<IActionResult> ResetChapters([FromRoute] string id, [FromRoute] int chapterId)
    {
        var worked = await _manga.ResetChapterPages(id, chapterId, this.UserFromIdentity()?.Id);
        return Ok(new { worked });
    }

    // GET manga/{id}/favourite
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

    // POST bookmark
    [HttpPost, Route("manga/{id}/{chapterId}/bookmark"), Authorize]
    public async Task<IActionResult> Bookmark([FromRoute] long id, [FromRoute] long chapterId, [FromBody] int[] pages)
    {
        var pid = this.UserFromIdentity()?.Id;
        if (string.IsNullOrEmpty(pid)) return Unauthorized();

        await _db.Manga.Bookmark(id, chapterId, pages, pid);
        return Ok();
    }

    // GET chapter/{id}/download
    [HttpGet, Route("manga/{id}/{chapterId}/download")]
    public async Task<IActionResult> DownloadChapter([FromRoute] string id, [FromRoute] int chapterId)
    {
        var result = await _manga.CreateZip(id, chapterId, this.UserFromIdentity()?.Id);
        if (result == null) return NotFound();

        var (stream, file) = result.Value;
        return File(stream, "application/zip", file);
    }

    // DELETE progress/{id}
    [HttpDelete, Route("manga/progress/{id}"), Authorize]
    public async Task<IActionResult> RemoveProgress([FromRoute] string id)
    {
        var pid = this.UserFromIdentity()?.Id;
        if (string.IsNullOrEmpty(pid)) return Unauthorized();

        var profile = await _db.Profiles.Fetch(pid);
        if (profile == null) return Unauthorized();

        var manga = await _db.Manga.GetManga(id, pid);
        if (manga == null || manga.Manga == null) return NotFound();

        await _db.Manga.DeleteProgress(profile.Id, manga.Manga.Id);
        return Ok();
    }

    // GET progress/{id}
    [HttpGet, Route("manga/{id}/progress"), Authorize]
    [ProducesDefaultResponseType(typeof(DbMangaProgress)), ProducesResponseType(404)]
    public async Task<IActionResult> GetProgress([FromRoute] string id)
    {
        var platform = this.UserFromIdentity()?.Id;
        if (string.IsNullOrEmpty(platform)) return BadRequest();

        var progress = long.TryParse(id, out long mid) ?
            await _db.Manga.GetProgress(platform, mid) :
            await _db.Manga.GetProgress(platform, id);
        if (progress == null) return NotFound();

        return Ok(progress);
    }

    // PUT progress
    [HttpGet, Route("manga/{id}/mark-as-read"), Authorize]
    public async Task<IActionResult> MarkAsRead([FromRoute] string id)
    {
        var pid = this.UserFromIdentity()?.Id;
        if (pid == null) return BadRequest();

        var worked = await _manga.ToggleRead(id, pid);
        return Ok(new { worked });
    }

    // PUT progress
    [HttpGet, Route("manga/{id}/mark-as-read/{chapterId}"), Authorize]
    public async Task<IActionResult> MarkAsReadChapter([FromRoute] string id, [FromRoute] long chapterId)
    {
        var pid = this.UserFromIdentity()?.Id;
        if (pid == null) return BadRequest();

        var worked = await _manga.ToggleRead(id, pid, chapterId);
        return Ok(new { worked });
    }

    // PUT progress
    [HttpPost, Route("manga/{id}/mark-as-read"), Authorize]
    public async Task<IActionResult> MarkAsReadPost([FromRoute] string id, [FromBody] long[] chapters)
    {
        var pid = this.UserFromIdentity()?.Id;
        if (pid == null) return BadRequest();

        var worked = await _manga.ToggleRead(id, pid, chapters);
        return Ok(new { worked });
    }

    // PUT progress
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

    // GET volume/{id}
    [HttpGet, Route("manga/volumed/{id}"), Route("manga/{id}/volumed")]
    [ProducesDefaultResponseType(typeof(MangaData)), ProducesResponseType(404)]
    public async Task<IActionResult> GetVolumed([FromRoute] string id, [FromQuery] string? sort = null, [FromQuery] bool asc = true)
    {
        var actSort = ChapterSortColumn.Ordinal;
        if (Enum.TryParse<ChapterSortColumn>(sort, true, out var res))
            actSort = res;

        var pid = this.UserFromIdentity()?.Id;
        var vols = await _manga.Volumed(id, pid, actSort, asc);
        if (vols == null) return NotFound();

        return Ok(vols);
    }

    // GET extended/{id}
    [HttpGet, Route("manga/{id}")]
	[ProducesDefaultResponseType(typeof(MangaWithChapters)), ProducesResponseType(404)]
	public async Task<IActionResult> Get([FromRoute] string id)
	{
		var pid = this.UserFromIdentity()?.Id;
		var manga = long.TryParse(id, out long mid) ?
			await _manga.Manga(mid, pid) :
			await _db.Manga.GetManga(id, pid);

		if (manga == null) return NotFound();
		return Ok(manga);
	}

    // GET search/{id}
	[HttpGet, Route("manga/{id}/extended")]
	[ProducesDefaultResponseType(typeof(MangaProgress)), ProducesResponseType(404)]
	public async Task<IActionResult> GetExt([FromRoute] string id)
	{
		var pid = this.UserFromIdentity()?.Id;
		var manga = long.TryParse(id, out long mid) ?
			await _db.Manga.GetMangaExtended(mid, pid) :
			await _db.Manga.GetMangaExtended(id, pid);

		if (manga == null) return NotFound();
		return Ok(manga);
    }

    // GET search/touched
    [HttpGet, Route("manga/touched")]
    [ProducesDefaultResponseType(typeof(PaginatedResult<MangaProgress>))]
    public Task<IActionResult> Touched([FromQuery] int page = 1, [FromQuery] int size = 100, [FromQuery] string? type = null)
    {
        if (!Enum.TryParse<TouchedState>(type, true, out var touchedType))
            touchedType = TouchedState.All;

        var search = new MangaFilter()
        {
            Page = page,
            Size = size,
            State = touchedType
        };

        return SearchV2(search);
    }

    // GET search/since/{date}
    [HttpGet, Route("manga/since/{date}")]
    [ProducesDefaultResponseType(typeof(PaginatedResult<MangaProgress>))]
    public async Task<IActionResult> Since([FromRoute] DateTime date, [FromQuery] int page = 1, [FromQuery] int size = 100)
    {
        var records = await _db.Manga.Since(this.UserFromIdentity()?.Id, date, page, size);
        return Ok(records);
    }

    [HttpGet, Route("manga/{id}/download")]
	public async Task<IActionResult> Download([FromRoute] long id)
	{
		var data = await _epub.Generate(id);
		if (data == null) return NotFound();

		return File(data.Stream, data.Mimetype, data.Name);
	}

	[HttpGet, Route("manga/refresh")]
	[ProducesDefaultResponseType(typeof(MangaWorked[]))]
	public async Task<IActionResult> Refresh([FromQuery] int count = 5)
	{
		var data = await _manga.Updated(count, this.UserFromIdentity()?.Id);
		return Ok(data);
	}

	[HttpPost, Route("manga/strip")]
	public async Task<IActionResult> Strip([FromBody] MangaStripRequest req)
	{
		var stream = await _image.Combine(req);
		if (stream == null) return BadRequest();

		return File(stream, "image/png", "output.png");
	}

	[HttpGet, Route("manga/graph")]
	[ProducesDefaultResponseType(typeof(GraphOut[]))]
	public async Task<IActionResult> Graph([FromQuery] string? state = null)
	{
		if (!Enum.TryParse<TouchedState>(state, true, out var touchedType))
			touchedType = TouchedState.Completed;

		var records = await _db.Manga.Graphic(this.UserFromIdentity()?.Id, touchedType);
		return Ok(records);
	}

	[HttpPost, Route("manga/saucenao")]
	public async Task<IActionResult> Sauce([FromBody] SauceRequest request)
	{
		var res = await _sauce.Get(request.ImageUrl, request.Databases);
		return Ok(res);
	}

	[HttpGet, Route("manga/{id}/index")]
	public async Task<IActionResult> IndexPages([FromRoute] string id)
	{
		var res = await _match.IndexManga(id);
		return Ok(new
		{
			worked = res
		});
	}

	[HttpGet, Route("manga/fix-bad-covers")]
	public async Task<IActionResult> FixBadCovers()
	{
		var res = await _match.FixCoverArt();
		return Ok(new
		{
			count = res
		});
	}

    [HttpDelete, Route("manga/{id}"), AdminAuthorize]
    public async Task<IActionResult> DeleteManga([FromRoute] long id)
    {
        var res = await _db.Manga.Get(id);
        if (res == null) return NotFound();

        await _db.Manga.DeleteManga(res.Id);
        return Ok();
    }

    [HttpDelete, Route("manga/{id}/{chapterId}"), AdminAuthorize]
    public async Task<IActionResult> DeleteChapter([FromRoute] long id, [FromRoute] long chapterId)
    {
        var res = await _db.Manga.Get(id);
        if (res == null) return NotFound();

        var chapter = await _db.Manga.GetChapter(chapterId);
        if (chapter == null) return NotFound();

        await _db.Manga.DeleteChapter(chapter.Id);
        return Ok();
    }
}

public class DisplayTitleRequest
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("title")]
	public string? Title { get; set; }
}

public class OrdinalResetRequest
{
	[JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

	[JsonPropertyName("reset")]
    public bool Reset { get; set; }
}

public class SauceRequest
{
	[JsonPropertyName("url")]
	public string ImageUrl { get; set; } = string.Empty;

	[JsonPropertyName("databases")]
	public SauceNaoDatabase[] Databases { get; set; } = Array.Empty<SauceNaoDatabase>();
}