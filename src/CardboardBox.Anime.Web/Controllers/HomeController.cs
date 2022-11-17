using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Web.Controllers
{
	using Services;

	public class HomeController : Controller
	{
		private readonly IOpenGraphService _og;

		public HomeController(IOpenGraphService og)
		{
			_og = og;
		}

		[HttpGet, Route("/")]
		public async Task<IActionResult> Index()
		{
			var data = await _og.Replace(c =>
			{
				c.Title("Hello world")
				 .Description("This is a test thing");
			});
			return File(data, "text/html");
		}
	}
}