﻿using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Web.Controllers;

using Database;
using LightNovel.Core;
using Services;

public class HomeController : Controller
{
	private readonly IOpenGraphService _og;
	private readonly IDbService _db;
	private readonly ILnDbService _novels;

	private IMangaDbService _mangaDb => _db.Manga;

	private const string DEFAULT_IMAGE = "https://cba.index-0.com/assets/logo.png";
	private const string SITE_NAME_MANGA = "CardboardBox | Manga";
	private const string SITE_NAME_ANIME = "CardboardBox | Anime";
	private const string SITE_NAME_BOOKS = "CardboardBox | Books";

	public HomeController(
		IOpenGraphService og, 
		IDbService db,
		ILnDbService novels)
	{
		_og = og;
		_db = db;
		_novels = novels;
	}

	[HttpGet, Route("/")]
	public async Task<IActionResult> Index()
	{
		var data = await _og.Replace(c =>
		{
			c.Title(SITE_NAME_ANIME)
			 .Description("Search for your next anime, manga, or light novel binge. With the added benefit of being able to generate AI images!")
			 .Type("website")
			 .Image(DEFAULT_IMAGE)
			 .Url("https://cba.index-0.com");
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("manga/{id}")]
	public async Task<IActionResult> Manga(string id)
	{
		var manga = await _mangaDb.GetManga(id, null);
		if (manga == null)
			return File(await _og.Default(), "text/html");

		var data = await _og.Replace(c =>
		{
			c.Title(manga.Manga.Title)
			 .Description(manga.Manga.Description)
			 .Locale("en-us")
			 .Type("website")
			 .Url("https://cba.index-0.com/manga/" + id)
			 .SiteName(SITE_NAME_MANGA)
			 .ProxiedImage(manga.Manga.Cover)
			 .PublishTime(manga.Manga.CreatedAt ?? DateTime.Now)
			 .ModifiedTime(manga.Manga.UpdatedAt ?? DateTime.Now);
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("manga/{id}/{chapId}/{page}")]
	public async Task<IActionResult> Manga(string id, long chapId, int page)
	{
		page--;
		var manga = await _mangaDb.GetManga(id, null);
		var chap = manga?.Chapters?.FirstOrDefault(t => t.Id == chapId);
		if (manga == null || chap == null)
			return File(await _og.Default(), "text/html");

		var pages = chap.Pages;
		if (page < 0 || page >= pages.Length)
			return File(await _og.Default(), "text/html");

		var image = pages[page];

		var data = await _og.Replace(c =>
		{
			c.Title($"{manga.Manga.Title} - Ch. {chap.Ordinal} - {chap.Title}")
			 .Description(manga.Manga.Description)
			 .Locale("en-us")
			 .Type("website")
			 .Url($"https://cba.index-0.com/manga/{id}/{chapId}/{page}")
			 .SiteName(SITE_NAME_MANGA)
			 .ProxiedImage(image)
			 .PublishTime(chap.CreatedAt ?? DateTime.Now)
			 .ModifiedTime(chap.UpdatedAt ?? DateTime.Now);
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("manga"), /*Route("manga/{**catchall}")*/]
	public async Task<IActionResult> Manga()
	{
		var data = await _og.Replace(c =>
		{
			c.Title(SITE_NAME_MANGA)
			 .Description("Find your next manga binge!")
			 .Url("https://cba.index-0.com/manga/in-progress")
			 .SiteName(SITE_NAME_MANGA)
			 .Image(DEFAULT_IMAGE)
			 .Type("website");
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("anime/{**catchAll}"), Route("anime")]
	public async Task<IActionResult> Anime()
	{
		var data = await _og.Replace(c =>
		{
			c.Title(SITE_NAME_ANIME)
			 .Description("Find your next anime binge!")
			 .Url("https://cba.index-0.com/anime/all")
			 .SiteName(SITE_NAME_ANIME)
			 .Image(DEFAULT_IMAGE)
			 .Type("website");
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("ai"), Route("ai/{**catchAll}")]
	public async Task<IActionResult> Ai()
	{
		var data = await _og.Replace(c =>
		{
			c.Title(SITE_NAME_ANIME)
			 .Description("Generate all the AI images your heart desires")
			 .SiteName(SITE_NAME_ANIME)
			 .Type("website")
			 .Image(DEFAULT_IMAGE)
			 .Url("https://cba.index-0.com/ai");
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("series/{id}")]
	public async Task<IActionResult> Series(long id)
	{
		var series = await _novels.Series.Fetch(id);
		if (series == null) return File(await _og.Default(), "text/html");

		var data = await _og.Replace(c =>
		{
			c.Title(series.Title)
			 .Description(series.Description ?? series.Title)
			 .Locale("en-us")
			 .Type("website")
			 .Url("https://cba.index-0.com/series/" + id)
			 .SiteName(SITE_NAME_BOOKS)
			 .ProxiedImage(series.Image ?? DEFAULT_IMAGE)
			 .PublishTime(series.CreatedAt ?? DateTime.Now)
			 .ModifiedTime(series.UpdatedAt ?? DateTime.Now);
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("series/{id}/book/{bookId}")]
	public async Task<IActionResult> Series(long id, long bookId)
	{
		var series = await _novels.Books.Fetch(bookId);
		if (series == null) return File(await _og.Default(), "text/html");

		var data = await _og.Replace(c =>
		{
			c.Title(series.Title)
			 .Description($"Read `{series.Title}` online or download the ebooks!")
			 .Locale("en-us")
			 .Type("website")
			 .Url($"https://cba.index-0.com/series/{id}/book/{bookId}")
			 .SiteName(SITE_NAME_BOOKS)
			 .ProxiedImage(series.CoverImage ?? DEFAULT_IMAGE)
			 .PublishTime(series.CreatedAt ?? DateTime.Now)
			 .ModifiedTime(series.UpdatedAt ?? DateTime.Now);
		});
		return File(data, "text/html");
	}

	[HttpGet, Route("series/{id}/book/{bookId}/chapter/{chapterId}")]
	public async Task<IActionResult> Series(long id, long bookId, long chapterId)
	{
		var series = await _novels.Books.Fetch(bookId);
		if (series == null) return File(await _og.Default(), "text/html");

		var data = await _og.Replace(c =>
		{
			c.Title(series.Title)
			 .Description($"Read `{series.Title}` online or download the ebooks!")
			 .Locale("en-us")
			 .Type("website")
			 .Url($"https://cba.index-0.com/series/{id}/book/{bookId}/chapter/{chapterId}")
			 .SiteName(SITE_NAME_BOOKS)
			 .ProxiedImage(series.CoverImage ?? DEFAULT_IMAGE)
			 .PublishTime(series.CreatedAt ?? DateTime.Now)
			 .ModifiedTime(series.UpdatedAt ?? DateTime.Now);
		});
		return File(data, "text/html");
	}

	//[HttpGet, Route("series/{**catchAll}"), Route("series")]
	//public async Task<IActionResult> Series()
	//{
	//	var data = await _og.Replace(c =>
	//	{
	//		c.Title(SITE_NAME_BOOKS)
	//		 .Description("Find your next light novel binge!")
	//		 .Url("https://cba.index-0.com/series")
	//		 .SiteName(SITE_NAME_BOOKS)
	//		 .Image(DEFAULT_IMAGE)
	//		 .Type("website");
	//	});
	//	return File(data, "text/html");
	//}
}