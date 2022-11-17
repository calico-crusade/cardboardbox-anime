using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Web.Controllers
{
	using Database;
	using Services;

	[Route("[controller]")]
	public class MangaController : Controller
	{
		private readonly IOpenGraphService _og;
		private readonly IMangaDbService _db;

		public MangaController(
			IOpenGraphService og, 
			IMangaDbService db)
		{
			_og = og;
			_db = db;
		}

		[HttpGet, Route("{id}")]
		public async Task<IActionResult> Index(long id)
		{
			var manga = await _db.GetManga(id, null);
			if (manga == null)
				return File(await _og.Default(), "text/html");

			var data = await _og.Replace(c =>
			{
				c.Title(manga.Manga.Title)
				 .Description(manga.Manga.Description)
				 .Locale("en-us")
				 .Type("article")
				 .Url("https://cba.index-0.com/manga/" + id)
				 .SiteName("CardboardBox | Anime")
				 .ProxiedImage(manga.Manga.Cover)
				 .PublishTime(manga.Manga.CreatedAt ?? DateTime.Now)
				 .ModifiedTime(manga.Manga.UpdatedAt ?? DateTime.Now);
			});
			return File(data, "text/html");
		}

		[HttpGet, Route("{id}/{chapId}/{page}")]
		public async Task<IActionResult> Index(long id, long chapId, int page)
		{
			page--;
			var manga = await _db.GetManga(id, null);
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
				 .Type("article")
				 .Url($"https://cba.index-0.com/manga/{id}/{chapId}/{page}")
				 .SiteName("CardboardBox | Anime")
				 .ProxiedImage(image)
				 .PublishTime(chap.CreatedAt ?? DateTime.Now)
				 .ModifiedTime(chap.UpdatedAt ?? DateTime.Now);
			});
			return File(data, "text/html");
		}
	}
}
