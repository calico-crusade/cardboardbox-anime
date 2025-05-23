﻿namespace CardboardBox.LightNovel.Core;

using Sources;
using Sources.ZirusSource;

public interface INovelApiService
{
	ISourceService? Source(string url);
	Task<(int count, bool isNew)> Load(string url);
	Task<int> Load(long seriesId);
	Task<int> Load(Series series);
	Task<string?> Fix(long id, (int start, int? stop)[]? ranges = null);
	Chapter ChapterFromPage(Page page, Book book, long ordinal);
}

public class NovelApiService : INovelApiService
{
	private const int AUTO_BOOK_SPLIT = 9999;

	private readonly ISourceService[] _srcs;
	private readonly ILnDbService _db;

	public NovelApiService(
		ILnpSourceService lnSrc, 
		IShSourceService shSrc,
		IReLibSourceService rlSrc,
		ILntSourceService lntSrc,
		INyxSourceService nyxSrc,
		IZirusMusingsSourceService zirusSrc,
        INncSourceService nncSrc,
        IBakaPervertSourceService baka,
        IFanTransSourceService ftl,
        IHeadCanonTLSourceService headCanon,
		IMagicHouseSourceService magicHouse,
        IVampiramtlSourceService vampiramtl,
        IRoyalRoadSourceService royalRoad,
        IStorySeedlingSourceService storySeedling,
        ILnDbService db)
	{
		_db = db;
		_srcs = [lnSrc, shSrc, rlSrc, lntSrc, nyxSrc, zirusSrc, nncSrc, baka, ftl, headCanon, magicHouse, vampiramtl, royalRoad, storySeedling];
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
		if (src is ISourceVolumeService vsrc)
		{
			if (series == null) return (await LoadNewBookByVolume(url, vsrc), true);

			return (await CatchupBookByVolume(series, vsrc), false);
		}

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
		if (src is ISourceVolumeService vsrc)
            return CatchupBookByVolume(series, vsrc);
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

	public async Task<int> LoadNewBookByVolume(string url, ISourceVolumeService src)
	{
		var seriesInfo = await src.GetSeriesInfo(url);
		if (seriesInfo == null) return -1;

		var series = SeriesFromInfo(seriesInfo, url);
		series.Id = await _db.Series.Upsert(series);

		int bc = 0,
			po = 0;
		await foreach(var vol in src.Volumes(url))
		{
			bc++;
			var book = BookFromVolume(series, vol, bc);
			book.Id = await _db.Books.Upsert(book);

			int pc = 0;
			foreach(var chapInfo in vol.Chapters)
			{
				var chap = await src.GetChapter(chapInfo.Url, book.Title);
				if (chap == null) continue;

				pc++;
				po++;
				var pid = await _db.Pages.Upsert(PageFromChapter(chap, po, series.Id));
				var chapId = await _db.Chapters.Upsert(ChapterFromChapter(chap, book, pc));
				await _db.ChapterPages.Upsert(new ChapterPage
				{
					ChapterId = chapId,
					PageId = pid,
					Ordinal = 0
				});
			}
		}

		return po;
	}

	public async Task<int> CatchupBookByVolume(Series series, ISourceVolumeService src)
	{
		static IEnumerable<(Book book, Chapter chapter, ChapterPage map, Page page)> Flatten(FullScaffold scaffold)
        {
            foreach (var book in scaffold.Books)
                foreach (var chap in book.Chapters)
                    foreach (var page in chap.Pages)
                        yield return (book.Book, chap.Chapter, page.Map, page.Page);
        }

		static bool Exists<T>(IEnumerable<T> source, Func<T, bool> predicate, out T result)
		{
			foreach (var item in source)
			{
				if (!predicate(item)) continue;

				result = item;
				return true;
			}
            result = default!;
            return false;
        }

        //Get all of the books and chapters for the series
        var scaffold = await _db.Series.Scaffold(series.Id);
        if (scaffold == null) return -1;

        //Flatten the chapters and order them by ordinal
        var chapters = Flatten(scaffold)
			.OrderBy(t => t.page.Ordinal)
			.ToArray();

        //Fetch all of the volumes for the series
        var volumes = src.Volumes(series.Url);
        //Keep track of the volume index to be used as the ordinal for the book
        int vi = 0;
		//Keep track of the last page present
		Page? lastPage = null;
		//Track the number of new chapters loaded
		int loaded = 0;
		await foreach(var volume in volumes)
		{
			vi++;
			//Update the book information
			var book = BookFromVolume(series, volume, vi);
            book.Id = await _db.Books.Upsert(book);

			int chapOrdinal = 0;
			//Iterate through the chapters of the volume
			foreach(var chapter in volume.Chapters)
            {
                //Increment the chapter ordinal for the current book
                chapOrdinal++;
                //If the chapter already exists in the scaffold, skip it
                if (Exists(chapters, t => t.page.Url == chapter.Url, out var existing))
				{
					//Keep track of the last page so we can use the ordinal
					lastPage = existing.page;
					continue;
				}

				//Get the chapter from the source
				var chap = await src.GetChapter(chapter.Url, book.Title);
                if (chap == null) continue;
				//Increment the number of loaded chapters
                loaded++;
				//Determiner the ordinal of the page
				var ordinal = (lastPage?.Ordinal ?? 0) + 1;
                //Create the new page from the chapter
                var page = PageFromChapter(chap, ordinal, series.Id);
				page.Id = await _db.Pages.Upsert(page);
				lastPage = page;
                //Create the new chapter from the chapter
                var chapId = await _db.Chapters.Upsert(ChapterFromChapter(chap, book, chapOrdinal));
				//Create the map between the chapter and the page
				await _db.ChapterPages.Upsert(new()
				{
					ChapterId = chapId,
                    PageId = page.Id,
                    Ordinal = 0
                });
            }
        }
		//Return the number of newly loaded chapters
		return loaded;
    }

	public async Task<int> CatchupBookByVolumeOld(Series series, ISourceVolumeService src)
	{
		var scaffold = await _db.Series.PartialScaffold(series.Id);
		if (scaffold == null) return -1;

		var lastBook = scaffold.Books.OrderByDescending(t => t.Book.Ordinal).FirstOrDefault();
		if (lastBook == null)
			return await LoadNewBookByVolume(series.Url, src);

		var lastPage = await _db.Pages.LastPage(series.Id);
		if (lastPage == null) return -2;

		int pc = 0,
			bc = 0;
		await foreach(var vol in src.Volumes(series.Url))
		{
			var matchingBook = scaffold.Books.FirstOrDefault(t => t.Book.Title == vol.Title);

			//Update any existing books
			if (matchingBook != null)
			{
				//No new chapters? skip it.
				if (matchingBook.Chapters.Length == vol.Chapters.Length) continue;

				var lastChap = matchingBook.Chapters.OrderByDescending(t => t.Ordinal).FirstOrDefault();
				if (lastChap == null) continue;

				var fc = vol.Chapters.Skip(matchingBook.Chapters.Length);
				int cc = 0;
				foreach (var chap in fc)
				{
					var chapter = await src.GetChapter(chap.Url, matchingBook.Book.Title);
					if (chapter == null) continue;

					cc++;
					pc++;
					var pid = await _db.Pages.Upsert(PageFromChapter(chapter, pc + lastPage.Ordinal, series.Id));
					var chapId = await _db.Chapters.Upsert(ChapterFromChapter(chapter, matchingBook.Book, lastChap.Ordinal + cc));
					await _db.ChapterPages.Upsert(new ChapterPage
					{
						ChapterId = chapId,
						PageId = pid,
						Ordinal = 0
					});
				}

				continue;
			}

			bc++;
			var book = BookFromVolume(series, vol, lastBook.Book.Ordinal + bc);
			book.Id = await _db.Books.Upsert(book);

			int bpc = 0;
			foreach(var chap in vol.Chapters)
			{
				var chapter = await src.GetChapter(chap.Url, book.Title);
				if (chapter == null) continue;

				pc++;
				bpc++;
				var pid = await _db.Pages.Upsert(PageFromChapter(chapter, lastPage.Ordinal + pc, series.Id));
				var chapId = await _db.Chapters.Upsert(ChapterFromChapter(chapter, book, bpc));
				await _db.ChapterPages.Upsert(new ChapterPage
				{
					ChapterId = chapId,
					PageId = pid,
					Ordinal = 0
				});
			}

		}

		return pc;
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

	public Chapter ChapterFromPage(Page page, Book book, long ordinal)
	{
		return new Chapter
		{
			HashId = $"{page.Title}-{book.Ordinal - 1}-{ordinal}".MD5Hash(),
			Title = page.Title,
			Ordinal = ordinal,
			BookId = book.Id
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
			Authors = info.Authors,
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

	public Book BookFromVolume(Series info, SourceVolume volume, long ordinal)
	{
		string[] Images(string[] source)
		{
			if (source != null && source.Length > 0) return source;
			if (!string.IsNullOrEmpty(info.Image)) return [info.Image];
			return [];
		}

		string? Cover()
		{
			if (volume.Forwards.Length > 0) return volume.Forwards.First();
			if (volume.Inserts.Length > 0) return volume.Inserts.First();
			if (!string.IsNullOrEmpty(info.Image)) return info.Image;
			return null;
		}

		var forwards = Images(volume.Forwards);
		var inserts = Images(volume.Inserts);
		
		return new Book
		{
			SeriesId = info.Id,
			CoverImage = Cover(),
			Forwards = forwards,
			Inserts = inserts,
			Title = volume.Title,
			HashId = volume.Title.MD5Hash(),
			Ordinal = ordinal
		};
	}

	public IEnumerable<Page[]> Chunk(IEnumerable<Page> pages, (int start, int? stop)[]? ranges = null)
	{
		var cur = new List<Page>();
		if (ranges == null)
		{
			int count = 0;
			foreach(var item in pages)
			{
				if (count >= AUTO_BOOK_SPLIT)
				{
					yield return cur.ToArray();
					cur.Clear();
					count = 0;
				}	

				cur.Add(item);
				count++;
			}

			if (cur.Count > 0)
				yield return cur.ToArray();
			yield break;
		}

		int r = 0;
		int i = 0;
		foreach(var page in pages)
		{
			if (r >= ranges.Length) break;

			var (start, stop) = ranges[r];
			if (i + 1 < start || (stop != null && i + 1 > stop))
			{
				r++;
				yield return cur.ToArray();
				cur.Clear();
			}

			cur.Add(page);
			i++;
		}

		if (cur.Count > 0)
			yield return cur.ToArray();
	}

	public async Task<string?> Fix(long id, (int start, int? stop)[]? ranges = null)
	{
		var series = await _db.Series.Fetch(id);
		if (series == null) return $"Couldn't find series with id: {id}";

		var (_, _, pages) = await _db.Pages.Paginate(id, 1, 10000);

		var src = Source(series.Url);
		if (src == null) return $"Couldn't figure out source for: {id} :: {series.Url}";

		var seriesInfo = await src.GetSeriesInfo(series.Url);
		if (seriesInfo == null || string.IsNullOrEmpty(seriesInfo.FirstChapterUrl)) return $"Coulnd't determine series info for: {id}";


		var chunks = Chunk(pages, ranges).ToArray();

		for(var i = 0; i < chunks.Length; i++)
		{
			var book = BookFromInfo(seriesInfo, id, i + 1);
			book.Id = await _db.Books.Upsert(book);

			var chunk = chunks[i];
			for(var c = 0; c < chunk.Length; c++)
			{
				var page = chunk[c];
				var chapId = await _db.Chapters.Upsert(ChapterFromPage(page, book, c + 1));
				await _db.ChapterPages.Upsert(new ChapterPage
				{
					ChapterId = chapId,
					PageId = page.Id,
					Ordinal = 0
				});
			}
		}

		return null;
	}
}
