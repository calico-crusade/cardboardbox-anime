using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers;

using Anime.Core;

[ApiController]
public class MiscController : ControllerBase
{
	private readonly IFileCacheService _cache;

	public MiscController(IFileCacheService cache)
	{
		_cache = cache;
	}

	[HttpGet, Route("misc/file")]
	public async Task<IActionResult> GetFile([FromQuery] string path)
	{
		var (io, name, mime) = await _cache.GetFile(path);
		return File(io, mime, name);
	}
}
