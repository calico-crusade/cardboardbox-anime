using System.Net;

namespace CardboardBox.Anime.Bot.Commands
{
	public class MangaReadComponent : ComponentHandler
	{
		private readonly IMangaApiService _api;
		private readonly IMangaUtilityService _util;
		private readonly IComponentService _comp;

		public MangaReadComponent(
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

		public (long id, long chapter, int page) GetState()
		{
			var parts = Message?.Content
				.Split(')')[0]
				.Split('(')
				.Last()
				.Split('/')
				.Reverse()
				.ToArray() ?? Array.Empty<string>();
			return (long.Parse(parts[2]), long.Parse(parts[1]), int.Parse(parts[0]) - 1);
		}

		public async Task<string[]> GetPages(DbManga manga, DbMangaChapter chapter)
		{
			if (chapter.Pages.Length > 0) return chapter.Pages;
			return await _api.GetPages(manga.Id, chapter.Id);
		}

		public async Task Do(bool next)
		{
			if (!await Validate()) return;

			if (Message == null)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... Message is null?";
				});
				return;
			}

			var (id, chap, page) = GetState();

			var manga = await _api.GetManga(id);
			if (manga == null || manga.Manga == null || manga.Chapters == null || manga.Chapters.Length == 0)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... Ran into an issue with fetching manga states...";
				});
				return;
			}

			var ci = manga.Chapters.IndexOf(t => t.Id == chap);
			var chapter = manga.Chapters[ci];
			var pages = await GetPages(manga.Manga, chapter);
			if (pages.Length == 0)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... Couldn't figure out the page you're on.";
				});
				return;
			}

			var pi = page + (next ? 1 : -1);
			if (pi < 0)
			{
				if (ci == 0)
				{
					await Acknowledge();
					return;
				}

				chapter = manga.Chapters[ci - 1];
				pages = await GetPages(manga.Manga, chapter);
				pi = pages.Length - 1;
			}
			else if (pi >= pages.Length)
			{
				if (ci == manga.Chapters.Length - 1)
				{
					await Acknowledge();
					return;
				}

				chapter = manga.Chapters[ci + 1];
				pages = await GetPages(manga.Manga, chapter);
				pi = 0;
			}

			using var img = await _util.GetImage(pages[pi]);

			var msgText = _util.GenerateRead(manga, chapter, pages, pi, UserId);
			var comp = await _comp.Components<MangaReadComponent>(Message);
			await Update(t =>
			{
				t.Content = msgText;
				t.Components = comp;
				t.Attachments = new[] { new FileAttachment(img, $"page-{manga.Manga.Id}-{chapter.Id}-{pi}.jpg") };
			});
		}

		public async Task DoChap(bool next)
		{
			if (!await Validate()) return;

			if (Message == null)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... Message is null?";
				});
				return;
			}

			var (id, chap, page) = GetState();

			var manga = await _api.GetManga(id);
			if (manga == null || manga.Manga == null || manga.Chapters == null || manga.Chapters.Length == 0)
			{
				await RemoveComponents(t =>
				{
					t.Content = "Uh... Ran into an issue with fetching manga states...";
				});
				return;
			}

			var ci = manga.Chapters.IndexOf(t => t.Id == chap);
			var i = ci + (next ? 1 : -1);

			if (i < 0) { await Acknowledge(); return; }
			if (i >= manga.Chapters.Length) { await Acknowledge(); return; }

			var chapter = manga.Chapters[i];
			var pages = await GetPages(manga.Manga, chapter);
			var pi = ci > i ? pages.Length - 1 : 0;

			using var img = await _util.GetImage(pages[pi]);

			var msgText = _util.GenerateRead(manga, chapter, pages, pi, UserId);
			var comp = await _comp.Components<MangaReadComponent>(Message);
			await Update(t =>
			{
				t.Content = msgText;
				t.Components = comp;
				t.Attachments = new[] { new FileAttachment(img, $"page-{manga.Manga.Id}-{chapter.Id}-{pi}.jpg") };
			});
		}

		[Button("Chap -", "⏮️")] public Task PreviousChapter() => DoChap(false);

		[Button("Page -", "⬅️")] public Task Previous() => Do(false);

		[Button("Page +", "➡️")] public Task Next() => Do(true);

		[Button("Chap +", "⏭️")] public Task NextChapter() => DoChap(true);

		[Button("Cancel", "⚔️", ButtonStyle.Danger)]
		public async Task Cancel()
		{
			if (!await Validate()) return;
			await RemoveComponents();
		}
	}
}
