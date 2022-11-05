namespace CardboardBox.Manga
{
	using Providers;

	public interface IMangaService
	{
		IMangaSource[] Sources();
		(IMangaSource? source, string? id) DetermineSource(string url);
	}

	public class MangaService : IMangaService
	{
		private readonly IMangakakalotSource _mangakakalot;

		public MangaService(IMangakakalotSource mangakakalot)
		{
			_mangakakalot = mangakakalot;
		}

		public IMangaSource[] Sources()
		{
			return new[]
			{
				_mangakakalot
			};
		}

		public (IMangaSource? source, string? id) DetermineSource(string url)
		{
			foreach (var source in Sources())
			{
				var (worked, id) = source.MatchesProvider(url);
				if (worked) return (source, id);
			}

			return (null, null);
		}
	}
}
