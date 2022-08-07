using CardboardBox.Http;
using Microsoft.Extensions.Logging;

namespace CardboardBox.Anime.HiDive
{
	using Core;
	using Core.Models;
	using HtmlAgilityPack;

	public interface IHiDiveApiService : IAnimeApiService
	{
		IAsyncEnumerable<Anime> Fetch(string url, string type = "series");
	}

	public class HiDiveApiService : IHiDiveApiService
	{
		private readonly ILogger _logger;
		private readonly IApiService _api;

		public HiDiveApiService(ILogger<HiDiveApiService> logger, IApiService api)
		{
			_logger = logger;
			_api = api;
		}

		public async IAsyncEnumerable<Anime> Fetch(string url, string type = "series")
		{
			var html = await _api.Create(url).GetHtml();
			if (html == null) yield break;

			var divs = html.DocumentNode.SelectNodes("//div[@class='body-bg-color top-page-offset']/div/div[@class='section']");
			foreach (var div in divs)
			{
				var cells = div.Copy().SelectNodes("//div[@class='title-slider show-list']/div[@class='cell']");
				if (cells == null) continue;

				foreach (var cell in cells)
				{
					var id = cell.GetAttributeValue("data-id", "");
					var anime = await FromElement(id, type, cell);
					if (anime == null) yield break;

					yield return anime.Clean();
				}
			}
		}

		public Task<HtmlDocument?> GetAnimeData(string id)
		{
			return _api.Create("https://www.hidive.com/shows/titlewindowcontent", "POST")
				.Body(("id", id))
				.GetHtml();
		}

		public async Task<Anime?> FromElement(string id, string type, HtmlNode rawNode)
		{
			var node = await GetAnimeData(id);
			if (node == null)
			{
				_logger.LogError($"Error occurred while fetching {type}::ID::{id}");
				return null;
			}
			var target = node.DocumentNode.SelectSingleNode("//div[@class='display-table']/div/div/h1/a");
			var title = target.InnerText;

			var metaels = node.DocumentNode.SelectNodes("//div[@class='display-table']/div/div/ul[@class='list-unstyled details']/li");
			var items = new Dictionary<string, string[]>();

			foreach(var el in metaels)
			{
				var text = el.InnerText;
				var parts = text.Split(':');
				var key = parts[0].Trim();
				var rest = string.Join(":", parts.Skip(1)).Split(new[] { "\r\n", "," }, StringSplitOptions.RemoveEmptyEntries);

				items.Add(key, rest.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray());
			}

			var getKey = (string key) => items.ContainsKey(key) ? items[key].ToList() : new List<string>();
			var audio = getKey("Audio");

			var types = new List<string>();
			if (audio.Contains("Japanese"))
				types.Add("Subbed");
			if (audio.Contains("English"))
				types.Add("Dubbed");
			if (types.Count == 0)
				types.Add("Unknown");

			var bgi = "https:" + node.DocumentNode.SelectSingleNode("//div[@class='table-cell title-img']").GetAttributeValue("style", "").Split(new[] { '(', ')' }).Skip(1).First();

			var poster = "https:" + rawNode.Copy().SelectSingleNode("//div[@class='default-img']/img").GetAttributeValue("data-src", "");
			var images = node.DocumentNode
				.SelectNodes("//div[@class='table-cell title-img']/div[@class='img-rotator']/img")
				.Select(t => ("https:" + t.GetAttributeValue("src", ""), t.GetAttributeValue("width", 0), t.GetAttributeValue("height", 0)))
				.ToArray();

			return new()
			{
				HashId = $"hidive-{id}-{title}".MD5Hash(),
				AnimeId = id,
				Link = "https://hidive.com" + target.GetAttributeValue("href", ""),
				Title = title,
				Description = node.DocumentNode.SelectSingleNode("//div[@class='display-table']/div/div/p[@class='hidden-xs']").InnerText,
				PlatformId = "hidive",
				Type = type,
				Metadata = new()
				{
					Languages = audio,
					Tags = getKey("Genres"),
					LanguageTypes = types,
					Mature = false,
					Seasons = new(),
					Ext = items.ToDictionary(t => t.Key, t => string.Join(",", t.Value))
				},
				Images = images.Select(t => new Image
					{
						Source = t.Item1,
						Width = t.Item2,
						Height = t.Item3,
						Type = "wallpaper",
						PlatformId = "none"
					}).Append(new Image
					{
						Source = bgi,
						Width = 512,
						Height = 288,
						Type = "wallpaper",
						PlatformId = "background-image"
					}).Append(new Image
					{
						Source = poster,
						Width = 300,
						Height = 169,
						Type = "poster",
						PlatformId = "poster"
					}).ToList()
			};
		}

		public async IAsyncEnumerable<Anime> All()
		{
			await foreach (var item in Fetch("https://www.hidive.com/tv/", "series"))
				yield return item;

			await foreach (var item in Fetch("https://www.hidive.com/movies/", "movies"))
				yield return item;
		}
	}
}