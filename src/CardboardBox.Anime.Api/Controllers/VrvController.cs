using Microsoft.AspNetCore.Mvc;

namespace CardboardBox.Anime.Api.Controllers
{
	using Vrv;

	[ApiController]
	public class VrvController : ControllerBase
	{
		[HttpGet, Route("vrv/all"), ProducesDefaultResponseType(typeof(VrvAnime[]))]
		public IActionResult All()
		{
			var io = System.IO.File.OpenRead("vrv-formatted.json");
			return File(io, "application/json");
		}
	}
}