namespace CardboardBox.Anime.Bot.Commands
{
	public class MangaCommand
	{
		private readonly IComponentService _components;
		private readonly IMangaApiService _api;
		private readonly IMangaUtilityService _util;

		public MangaCommand(
			IComponentService components,
			IMangaApiService api,
			IMangaUtilityService util)
		{
			_components = components;
			_api = api;
			_util = util;
		}

		[Command("manga", "Search for a manga available on https://cba.index-0.com/manga", LongRunning = true)]
		public async Task Manga(SocketSlashCommand cmd,
			[Option("Search Text", false)] string? search)
		{
			var filter = new MangaFilter
			{
				Search = search
			};
			var data = await _api.Search(filter);

			if (data == null || data.Count == 0 || data.Results.Length == 0)
			{
				await cmd.Modify("Couldn't find a manga that matches that search query!");
				return;
			}

			var manga = data.Results.First();
			var msg = await cmd.ModifyOriginalResponseAsync(f =>
			{
				f.Embed = _util.GenerateEmbed(manga)
					.Build();
				f.Content = "Search Text: " + search;
			});

			var comp = data.Count == 1 ?
				await _components.Components<MangaSearchReadComponent>(msg) :
				await _components.Components<MangaSearchComponent>(msg);
			await msg.ModifyAsync(f =>
			{
				f.Embed = _util.GenerateEmbed(manga)
					.AddOptField("Total Search Results", $"{1}/{data.Count}")
					.Build();
				f.Content = "Search Text: " + search;
				f.Components = comp;
			});
		}
	}

}
