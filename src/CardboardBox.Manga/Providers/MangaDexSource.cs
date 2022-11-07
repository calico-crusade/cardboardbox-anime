namespace CardboardBox.Manga.Providers
{
	using MangaDex;
	using MangaDex.Models;

	public interface IMangaDexSource : IMangaSource { }

	public class MangaDexSource : IMangaDexSource
	{
		private const string DEFAULT_LANG = "en";
		public string HomeUrl => "https://mangadex.org";
		public string Provider => "mangadex";

		private readonly IMangaDexService _mangadex;

		public MangaDexSource(IMangaDexService mangadex)
		{
			_mangadex = mangadex;
		}

		public async Task<MangaChapterPages?> ChapterPages(string mangaId, string chapterId)
		{
			var chapter = await _mangadex.Chapter(chapterId);
			if (chapter == null) return null;

			var pages = await _mangadex.Pages(chapterId);
			if (pages == null) return null;

			return new MangaChapterPages
			{
				Title = chapter.Data.Attributes.Title ?? string.Empty,
				Url = $"{HomeUrl}/chapter/{chapter.Data.Id}",
				Id = chapter.Data.Id ?? string.Empty,
				Number = double.TryParse(chapter.Data.Attributes.Chapter, out var a) ? a : 0,
				Pages = pages.Images
			};
		}

		public async Task<Manga?> Manga(string id)
		{
			var manga = await _mangadex.Manga(id);
			if (manga == null) return null;

			var coverFile = (manga.Data.Relationships.FirstOrDefault(t => t is CoverArtRelationship) as CoverArtRelationship)?.Attributes?.FileName;
			var coverUrl = $"{HomeUrl}/covers/{id}/{coverFile}";

			var chapters = await GetChapters(id, DEFAULT_LANG)
				.OrderBy(t => t.Number)
				.ToListAsync();

			return new Manga
			{
				Title = manga.Data.Attributes.Title.PreferedOrFirst(t => t.Key.ToLower() == DEFAULT_LANG).Value,
				Id = id,
				Provider = Provider,
				HomePage = $"{HomeUrl}/title/{id}",
				Cover = coverUrl,
				Tags = manga.Data
					.Attributes
					.Tags
					.Select(t => 
						t.Attributes
						 .Name
						 .PreferedOrFirst(t => t.Key == DEFAULT_LANG)
						 .Value).ToArray(),
				Chapters = chapters
			};
		}

		public async IAsyncEnumerable<MangaChapter> GetChapters(string id, params string[] languages)
		{
			var filter = new ChaptersFilter { TranslatedLanguage = languages };
			while(true)
			{
				var chapters = await _mangadex.Chapters(id, filter);
				if (chapters == null) yield break;

				var sortedChapters = chapters
					.Data
					.GroupBy(t => t.Attributes.Chapter)
					.Select(t => t.PreferedOrFirst(t => t.Attributes.TranslatedLanguage == DEFAULT_LANG))
					.Where(t => t != null)
					.Select(t =>
					{
						return new MangaChapter
						{
							Title = t?.Attributes.Title ?? string.Empty,
							Url = $"{HomeUrl}/chapter/{t?.Id}",
							Id = t?.Id ?? string.Empty,
							Number = double.TryParse(t?.Attributes.Chapter, out var a) ? a : 0
						};
					})
					.OrderBy(t => t.Number);

				foreach (var chap in sortedChapters)
					yield return chap;

				int current = chapters.Offset + chapters.Limit;
				if (chapters.Total <= current) yield break;

				filter.Offset = current;
			}
		}

		public (bool matches, string? part) MatchesProvider(string url)
		{
			var regex = new Regex("https://mangadex.org/title/(.*?)(/(.*?))?");
			if (!regex.IsMatch(url)) return (false, null);

			var parts = url.Split('/').Reverse().ToArray();
			
			var last = parts.Skip(1).First();
			if (last == "title")
				last = parts.First();
			return (true, last);
		}
	}
}
