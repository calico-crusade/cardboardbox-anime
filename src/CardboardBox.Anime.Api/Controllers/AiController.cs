﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IOFile = System.IO.File;

namespace CardboardBox.Anime.Api.Controllers;

using AI;
using Auth;
using Database;
using Match;
using Http;

[ApiController, Authorize]
public class AiController : ControllerBase
{
	public const int MIN_BATCH_COUNT = 1, MAX_BATCH_COUNT = 2,
					 MIN_BATCH_SIZE = 1, MAX_BATCH_SIZE = 4;
	public const long MAX_STEPS = 64, MIN_STEPS = 1,
					  MIN_SIZE = 64, MAX_SIZE = 1024;
	public const double MAX_CFG = 32, MIN_CFG = 1, 
					    MAX_DENOISE = 1.0, MIN_DENOISE = 0.1;

	private readonly IAiAnimeService _ai;
	private readonly IApiService _api;
	private readonly IDbService _db;
	private readonly INsfwApiService _nsfw;

	private static string ImageDir => Path.Combine("wwwroot", "image-cache");

	public AiController(
		IAiAnimeService ai, 
		IApiService api,
		IDbService db,
		INsfwApiService nsfw)
	{
		_ai = ai;
		_api = api;
		_db = db;
		_nsfw = nsfw;
	}

	[HttpGet, Route("ai/{id}")]
	[ProducesDefaultResponseType(typeof(DbAiRequest))]
	public async Task<IActionResult> AiRequest([FromRoute] long id)
	{
		var item = await _db.AiRequests.Fetch(id);
		if (item == null) return NotFound();

		return Ok(item);
	}

	[HttpGet, Route("ai")]
	[ProducesDefaultResponseType(typeof(PaginatedResult<DbAiRequest>))]
	public async Task<IActionResult> Requests([FromQuery] long? id = null, [FromQuery] int size = 100, [FromQuery] int page = 1)
	{
		var isAdmin = User.IsInRole("Admin");

		if (!isAdmin || id == -1)
		{
			var pid = this.UserFromIdentity()?.Id ?? "";
			var profile = await _db.Profiles.Fetch(pid);
			if (profile == null) return Unauthorized();

			id = profile.Id;
		}

		var data = await _db.AiRequests.Paged(id, page, size);
		return Ok(data);
	}

	[HttpPost, Route("ai")]
	public async Task<IActionResult> Text2Img([FromBody] AiRequest request, [FromQuery] bool download = false)
	{
		var valRes = Validate(request);
		if (valRes != null) return valRes;

		var req = await From(request);
		if (req == null) return Unauthorized();

		req.Id = await _db.AiRequests.Insert(req);

		var res = await _ai.Text2Img(request);
		if (res == null) return NotFound();

		req.GenerationEnd = DateTime.Now;
		req.SecondsElapsed = (long)(req.GenerationEnd - req.GenerationStart).Value.TotalSeconds;

		await _db.AiRequests.Update(req);

		if (res.Images.Length == 0) return NotFound();

		if (res.Images.Length == 1 && download)
		{
			var singleData = Convert.FromBase64String(res.Images[0]);
			return File(singleData, "image/png", "image.png");
		}

		var urls = await res.Images.Select(t => _ai.DecodeAndSaveUrl(t, ImageDir)).ToArray().WhenAll();

		req.OutputPaths = urls;
		await _db.AiRequests.Update(req);

		return Ok(new
		{
			id = req.Id
		});
	}

	[HttpPost, Route("ai/img")]
	public async Task<IActionResult> Img2Img([FromBody] AiRequestImg2Img request, [FromQuery] bool download = false)
	{
		var valRes = Validate(request);
		if (valRes != null) return valRes;

		var req = await From(request);
		if (req == null) return Unauthorized();

		req.Id = await _db.AiRequests.Insert(req);

		if (request.Images.Length == 0)
			return BadRequest();

		req.GenerationStart = DateTime.Now;

		var res = await _ai.Img2Img(request);

		if (res == null) return NotFound();

		req.GenerationEnd = DateTime.Now;
		req.SecondsElapsed = (long)(req.GenerationEnd - req.GenerationStart).Value.TotalSeconds;

		await _db.AiRequests.Update(req);

		if (res.Images.Length == 0) return NotFound();

		if (res.Images.Length == 1 && download)
		{
			var singleData = Convert.FromBase64String(res.Images[0]);
			return File(singleData, "image/png", "image.png");
		}

		var urls = await res.Images.Select(t => _ai.DecodeAndSaveUrl(t, ImageDir)).ToArray().WhenAll();

		req.OutputPaths = urls;
		await _db.AiRequests.Update(req);

		return Ok(new
		{
			id = req.Id
		});
	}

	[HttpGet, Route("ai/embeddings")]
	public async Task<IActionResult> Embeddings()
	{
		var embeds = (await _ai.Embeddings())
			.Where(t => Path.GetExtension(t).Trim('.').ToLower() != "txt")
			.Select(t => Path.GetFileNameWithoutExtension(t))
			.ToArray();
		return Ok(embeds);
	}

	[HttpGet, Route("ai/loras")]
	public async Task<IActionResult> Loras()
	{
		return Ok(await _ai.Loras());
	}

	[HttpGet, Route("ai/images"), AdminAuthorize]
	public IActionResult Images()
	{
		if (!Directory.Exists(ImageDir)) return NotFound();

		return Ok(Directory
			.GetFiles(ImageDir)
			.Select(t => string.Join("/", t.Split('/', '\\').Skip(1)))
			.ToArray());
	}

	[HttpGet, Route("ai/nsfw")]
	public async Task<IActionResult> Nsfw([FromQuery]string url)
	{
		var classifiction = await _nsfw.Get(url);
		return Ok(classifiction);
	}

	[HttpGet, Route("ai/samplers")]
	public async Task<IActionResult> Samplers()
	{
		return Ok(await _ai.Samplers());
	}

	private IActionResult? Validate(AiRequest request)
	{
		var validator = (string prop, double act, double min, double max) =>
		{
			if (act < min || act > max) return $"Image {prop} has to be between {min} and {max}";
			return null;
		};

		if (request == null) return NotFound(new { message = "Image request failed. Is it a valid URL?" });

		var validators = new List<string>
		{
			validator("width", request.Width, MIN_SIZE, MAX_SIZE),
			validator("height", request.Height, MIN_SIZE, MAX_SIZE),
			validator("steps", request.Steps, MIN_STEPS, MAX_STEPS),
			validator("CFG scale", request.CfgScale, MIN_CFG, MAX_CFG),
			validator("batch count", request.BatchCount, MIN_BATCH_COUNT, MAX_BATCH_COUNT),
			validator("batch size", request.BatchSize, MIN_BATCH_SIZE, MAX_BATCH_SIZE),
		};

		if (request is AiRequestImg2Img img2img)
		{
			validators.Add(validator("denoise strength", img2img.DenoiseStrength, MIN_DENOISE, MAX_DENOISE));
			
			if (string.IsNullOrEmpty(img2img.Image))
				validators.Add("Image URL cannot be blank!");
		}

		if (string.IsNullOrEmpty(request.Prompt))
			validators.Add("Image prompt cannot be blank!");

		var foundIssues = validators.Where(t => !string.IsNullOrEmpty(t)).ToArray();
		if (foundIssues.Length == 0) return null;

		return BadRequest(new
		{
			issues = foundIssues
		});
	}

	private async Task<DbAiRequest?> From(AiRequest request)
	{
		var pid = this.UserFromIdentity()?.Id ?? "";

		var profile = await _db.Profiles.Fetch(pid);
		if (profile == null) return null;

		var res = new DbAiRequest
		{
			ProfileId = profile.Id,
			Prompt = request.Prompt,
			NegativePrompt = request.NegativePrompt,
			Steps = request.Steps,
			BatchCount = request.BatchCount,
			BatchSize = request.BatchSize,
			CfgScale = request.CfgScale,
			Seed = request.Seed,
			Height = request.Height,
			Width = request.Width,
			GenerationStart = DateTime.Now,
			Sampler = request.Sampler,
		};

		if (request is AiRequestImg2Img img)
		{
			res.ImageUrl = img.Image;
			res.DenoiseStrength = img.DenoiseStrength;
		}

		return res;
	}
}
