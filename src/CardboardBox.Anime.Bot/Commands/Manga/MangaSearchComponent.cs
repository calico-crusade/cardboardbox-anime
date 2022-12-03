using CardboardBox.Anime.Bot.Services;

namespace CardboardBox.Anime.Bot.Commands
{
	public class MangaSearchComponent : MangaSearchReadComponent
	{
		private readonly IMangaApiService _api;
		private readonly IMangaUtilityService _util;
		private readonly IComponentService _comp;

		public MangaSearchComponent(
			IMangaApiService api, 
			IMangaUtilityService util, 
			IComponentService comp) : base(api, util, comp)
		{
			_api = api;
			_util = util;
			_comp = comp;
		}

		public async Task<(DbManga? manga, string search, long count, int index)> Get(ButtonTarget target)
		{
			var text = Message?.Content.Replace("Search Text:", "").Trim() ?? string.Empty;
			var embed = Message?.Embeds.FirstOrDefault()?.Url ?? string.Empty;
			var id = long.Parse(embed.Split('/').LastOrDefault() ?? "1");

			var filter = new MangaFilter
			{
				Search = text,
				Size = 100
			};
			var search = await _api.Search(filter);
			if (search == null || search.Count == 0 || search.Results.Length == 0) return (null, text, 0, 0);

			var index = search.Results.IndexOf(t => t.Manga.Id == id);
			if (index == -1) return (null, text, 0, 0);

			int i = index;
			switch(target)
			{
				case ButtonTarget.First: return (search.Results.First().Manga, text, search.Count, 0);
				case ButtonTarget.Last: return (search.Results.Last().Manga, text, search.Count, (int)search.Count - 1);
				case ButtonTarget.Next: i += 1; break;
				case ButtonTarget.Prev: i -= 1; break;
			}

			if (i < 0) return (search.Results.First().Manga, text, search.Count, 0);
			if (i >= search.Count) return (search.Results.Last().Manga, text, search.Count, (int)search.Count - 1);
			return (search.Results[i].Manga, text, search.Count, i);
		}

		public async Task Update(DbManga manga, string search, long count, int index)
		{
			if (Message == null) return;

			var comp = await _comp.Components<MangaSearchComponent>(Message);
			await Update(t =>
			{
				t.Content = "Search Text: " + search;
				t.Components = comp;
				t.Embed = _util.GenerateEmbed(manga).AddOptField("Total Search Results", $"{index + 1}/{count}").Build();
			});
		}

		public async Task Do(ButtonTarget target)
		{
			var (manga, text, count, index) = await Get(target);
			if (manga == null)
			{
				await Update(t =>
				{
					t.Content = "Couldn't find a manga for that target.";
				});
				return;
			}

			await Update(manga, text, count, index);
		}

		[Button("First", "⏮️")]
		public async Task First()
		{
			await Do(ButtonTarget.First);
		}

		[Button("Prev", "⬅️")]
		public async Task Previous()
		{
			await Do(ButtonTarget.Prev);
		}

		[Button("Next", "➡️")]
		public async Task Next()
		{
			await Do(ButtonTarget.Next);
		}

		[Button("Last", "⏭️")]
		public async Task Last()
		{
			await Do(ButtonTarget.Last);
		}

		public enum ButtonTarget
		{
			First = 1,
			Prev = 2,
			Next = 3,
			Last = 4
		}
	}

	public class MangaSearchReadComponent : ComponentHandler
	{
		private readonly IMangaApiService _api;
		private readonly IMangaUtilityService _util;
		private readonly IComponentService _comp;

		public MangaSearchReadComponent(
			IMangaApiService api,
			IMangaUtilityService util,
			IComponentService comp)
		{
			_api = api;
			_util = util;
			_comp = comp;
		}

		public async Task<bool> Validate()
		{
			if (Message?.Interaction.User.Id == User?.Id) return true;
			await Acknowledge();
			return false;
		}

		[Button("Close", "⚔️", ButtonStyle.Danger)]
		public async Task Close()
		{
			if (!await Validate()) return;

			await RemoveComponents();
		}

		[Button("Read", "📖", ButtonStyle.Success)]
		public async Task Read()
		{
			if (!await Validate()) return;

			var embed = Message?.Embeds.FirstOrDefault()?.Url ?? string.Empty;
			var id = long.Parse(embed.Split('/').LastOrDefault() ?? "1");

			if (Message == null)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... RIP";
				});
				return;
			}

			var manga = await _api.GetManga(id);
			if (manga == null)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... This is awkward... I couldn't find that manga :(";
				});
				return;
			}

			var chapter = manga.Chapters.First();
			var pages = chapter.Pages.Length > 0 ? chapter.Pages : await _api.GetPages(id, chapter.Id);
			if (pages == null || pages.Length == 0)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... This is awkward... I couldn't find the pages for that manga :(";
				});
				return;
			}

			var msgText = _util.GenerateRead(manga, chapter, pages, 0, UserId);
			using var img = await _util.GetImage(pages[0]);
			var comp = await _comp.Components<MangaReadComponent>(Message);
			await Update(t =>
			{
				t.Content = msgText;
				t.Embed = null;
				t.Components = comp;
				t.Attachments = new[] { new FileAttachment(img, $"page-{manga.Manga.Id}-{chapter.Id}-{0}.jpg") };
			});
		}
	}
}
