namespace CardboardBox.LightNovel.Core
{
	using Sources;

	public interface INovelApiService
	{
		ISourceService? Source(string url);
		Task<(int count, bool isNew)> Load(string url);
		Task<int> Load(long seriesId);
		Task<int> Load(Series series);
	}

	public class NovelApiService : INovelApiService
	{
		private const int AUTO_BOOK_SPLIT = 75;

		private readonly ISourceService[] _srcs;
		private readonly ILnDbService _db;

		public NovelApiService(
			ILnpSourceService lnSrc, 
			IShSourceService shSrc,
			ILnDbService db)
		{
			_db = db;
			_srcs = new[] { (ISourceService)lnSrc, shSrc };
		}

		public ISourceService? Source(string url)
		{
			var root = url.GetRootUrl().ToLower();
			return _srcs.FirstOrDefault(t => t.RootUrl.ToLower() == root);
		}

		public async Task<(int count, bool isNew)> Load(string url)
		{
			url = url.ToLower();
			var src = Source(url) ?? throw new NotSupportedException($"Could not find source loader for: {url}");

			var series = await _db.Series.FromUrl(url);
			if (series == null) return (await LoadNewBook(url, src), true);

			return (await CatchupBook(series, src), false);
		}

		public async Task<int> Load(long seriesId)
		{
			var series = await _db.Series.Fetch(seriesId);
			if (series == null) return -1;

			return await Load(series);
		}

		public Task<int> Load(Series series)
		{
			var src = Source(series.Url) ?? throw new NotSupportedException($"Could not find source loader for: {series.Url}");
			return CatchupBook(series, src);
		}

		public async Task<int> LoadNewBook(string url, ISourceService src)
		{
			var seriesInfo = await src.GetSeriesInfo(url);
			if (seriesInfo == null || string.IsNullOrEmpty(seriesInfo.FirstChapterUrl)) return -1;

			var seriesId = await _db.Series.Upsert(SeriesFromInfo(seriesInfo, url));

			var book = BookFromInfo(seriesInfo, seriesId, 1);
			book.Id = await _db.Books.Upsert(book);

			int count = -1,
				bookPages = -1;
			await foreach(var chap in src.Chapters(seriesInfo.FirstChapterUrl))
			{
				count++; bookPages++;
				var pageId = await _db.Pages.Upsert(PageFromChapter(chap, count + 1, seriesId));

				if (bookPages >= AUTO_BOOK_SPLIT)
				{
					bookPages = 0;
					book = BookFromInfo(seriesInfo, seriesId, book.Ordinal + 1);
					book.Id = await _db.Books.Upsert(book);
				}

				var chapId = await _db.Chapters.Upsert(ChapterFromChapter(chap, book, bookPages + 1));
				await _db.ChapterPages.Upsert(new ChapterPage
				{
					ChapterId = chapId,
					PageId = pageId,
					Ordinal = 0
				});
			}

			return count;
		}

		public async Task<int> CatchupBook(Series series, ISourceService src)
		{
			var lastPage = await _db.Pages.LastPage(series.Id);
			var (book, lastChap, _) = await _db.Chapters.LastChapter(series.Id);
			var chapOrd = lastChap?.Ordinal ?? 0;

			if (lastPage == null) throw new ArgumentNullException(nameof(lastPage), $"Last page is null for series: {series.Id} :: {series.Title}");

			var offset = -1;
			await foreach (var chap in src.Chapters(lastPage.Url))
			{
				offset++;
				var pageId = await _db.Pages.Upsert(PageFromChapter(chap, lastPage.Ordinal + offset, series.Id));

				if (book == null || lastChap == null) continue;

				chapOrd++;

				if (chapOrd >= AUTO_BOOK_SPLIT)
				{
					book = BookFromSeries(series, book.Ordinal + 1);
					book.Id = await _db.Books.Upsert(book);
					chapOrd = 1;
				}

				var chapId = await _db.Chapters.Upsert(ChapterFromChapter(chap, book, chapOrd));

				await _db.ChapterPages.Upsert(new ChapterPage
				{
					ChapterId = chapId,
					PageId = pageId,
					Ordinal = 0
				});
			}

			return offset;
		}

		public Page PageFromChapter(SourceChapter chap, long ordinal, long seriesId)
		{
			return new Page
			{
				HashId = chap.Url.MD5Hash(),
				Title = chap.ChapterTitle,
				Ordinal = ordinal,
				SeriesId = seriesId,
				Url = chap.Url,
				NextUrl = string.IsNullOrWhiteSpace(chap.NextUrl) ? null : chap.NextUrl,
				Content = chap.Content,
				Mimetype = "application/html"
			};
		}

		public Chapter ChapterFromChapter(SourceChapter chap, Book book, long ordinal)
		{
			return new Chapter
			{
				HashId = $"{chap.ChapterTitle}-{book.Ordinal - 1}-{ordinal}".MD5Hash(),
				Title = chap.ChapterTitle,
				Ordinal = ordinal,
				BookId = book.Id
			};
		}

		public Series SeriesFromInfo(TempSeriesInfo info, string url)
		{
			return new Series
			{
				HashId = info.Title.MD5Hash(),
				Title = info.Title,
				Url = url,
				LastChapterUrl = "",
				Image = info.Image,
				Genre = info.Genre,
				Tags = info.Tags,
				Authors = new[] { info.Author ?? "" },
				Illustrators = Array.Empty<string>(),
				Editors = Array.Empty<string>(),
				Translators = Array.Empty<string>(),
				Description = info.Description
			};
		}

		public Book BookFromInfo(TempSeriesInfo info, long seriesId, long volume)
		{
			var ia = string.IsNullOrEmpty(info.Image) ? Array.Empty<string>() : new[] { info.Image };
			var title = $"{info.Title} Vol {volume}";
			return new Book
			{
				SeriesId = seriesId,
				CoverImage = info.Image,
				Forwards = ia,
				Inserts = ia,
				Title = title,
				HashId = title.MD5Hash(),
				Ordinal = volume
			};
		}

		public Book BookFromSeries(Series info, long volume)
		{
			var ia = string.IsNullOrEmpty(info.Image) ? Array.Empty<string>() : new[] { info.Image };
			var title = $"{info.Title} Vol {volume}";

			return new Book
			{
				SeriesId = info.Id,
				CoverImage = info.Image,
				Forwards = ia,
				Inserts = ia,
				Title = title,
				HashId = title.MD5Hash(),
				Ordinal = volume
			};
		}
	}
}
