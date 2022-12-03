using SLImage = SixLabors.ImageSharp.Image;
using AImage = CardboardBox.Anime.Core.Models.Image;

namespace CardboardBox.Anime.Cli
{
	using Crunchyroll;
	using Core;
	using Core.Models;
	using Database;
	using Funimation;
	using HiDive;
	using Vrv;

	using LightNovel.Core;
	using LightNovel.Core.Sources;
	using LightNovel.Core.Sources.Utilities;

	using Manga.MangaDex;
	using Manga.Providers;

	public interface IRunner
	{
		Task<int> Run(string[] args);
	}

	public class Runner : IRunner
	{
		private const string VRV_JSON = "vrv2.json";
		private const string FUN_JSON = "fun.json";

		private readonly IVrvApiService _vrv;
		private readonly IFunimationApiService _fun;
		private readonly ILogger _logger;
		private readonly IApiService _api;
		private readonly IAnimeMongoService _mongo;
		private readonly IHiDiveApiService _hidive;
		private readonly IAnimeDbService _db;
		private readonly ICrunchyrollApiService _crunchy;
		private readonly IOldLnApiService _ln;
		private readonly IChapterDbService _chapDb;
		private readonly IPdfService _pdf;
		private readonly ILnDbService _lnDb;
		private readonly INovelApiService _napi;
		private readonly INovelUpdatesService _info;
		private readonly IReLibSourceService _reL;
		private readonly IMangaDexService _mangaDex;
		private readonly IMangaClashSource _mangaClash;
		private readonly INhentaiSource _nhentai;

		public Runner(
			IVrvApiService vrv, 
			ILogger<Runner> logger,
			IFunimationApiService fun,
			IApiService api,
			IAnimeMongoService mongo,
			IHiDiveApiService hidive,
			IAnimeDbService db,
			ICrunchyrollApiService crunchy,
			IOldLnApiService ln,
			IChapterDbService chapDb,
			IPdfService pdf,
			ILnDbService lnDb,
			INovelApiService napi,
			INovelUpdatesService info,
			IReLibSourceService reL,
			IMangaDexService mangaDex,
			IMangaClashSource mangaClash,
			INhentaiSource nhentai)
		{
			_vrv = vrv;
			_logger = logger;
			_fun = fun;
			_api = api;
			_mongo = mongo;
			_hidive = hidive;
			_db = db;
			_crunchy = crunchy;
			_ln = ln;
			_chapDb = chapDb;
			_pdf = pdf;
			_lnDb = lnDb;
			_napi = napi;
			_info = info;
			_reL = reL;
			_mangaDex = mangaDex;
			_mangaClash = mangaClash;
			_nhentai = nhentai;
		}

		public async Task<int> Run(string[] args)
		{
			try
			{
				_logger.LogInformation("Starting with args: " + string.Join(" ", args));
				var last = args.Last().ToLower();
				var command = string.IsNullOrEmpty(last) ? "fetch" : last;

				switch (command)
				{
					case "fetch": await FetchVrvResources(); break;
					case "format": await FormatVrvResources(); break;
					case "fun": await FetchFunimationResources(); break;
					case "sizes": await DeteremineImageSizes(); break;
					case "all": await All(); break;
					case "reformat": await ReformatIds(); break;
					case "load": await Load(); break;
					case "test": await Test(); break;
					case "clean": await Clean(); break;
					case "hidive": await Hidive(); break;
					case "migrate": await Migrate(); break;
					case "crunchy": await LoadCrunchy(); break;
					case "ln": await LoadLightNovel(); break;
					case "conv": await ToPdf(); break;
					case "epub": await ToEpub(); break;
					case "pickup": await PickupNew(); break;
					case "lnmigr": await DoJm(); break;
					case "mangadex": await TestMangaDex(); break;
					case "clash": await TestMangaClash(); break;
					default: _logger.LogInformation("Invalid command: " + command); break;
				}

				_logger.LogInformation("Finished.");
				return 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing command: " + string.Join(" ", args));
				return 1;
			}
		}

		public async Task Crunchy()
		{
			const string token = "";

			var data = await _crunchy.All(token).ToListAsync();
			if (data == null)
			{
				_logger.LogError("Failed to fetch crunchy data");
				return;
			}

			var ser = JsonSerializer.Serialize(data);
			await File.WriteAllTextAsync("crunchy.json", ser);

			_logger.LogInformation($"Data Results: {data?.Count}");
		}

		public async Task LoadCrunchy()
		{
			using var io = File.OpenRead("crunchy.json");
			var data = await JsonSerializer.DeserializeAsync<Anime[]>(io);

			if (data == null)
			{
				_logger.LogError("Data failed to load");
				return;
			}	

			foreach (var anime in data)
				await _db.Upsert(anime);

			_logger.LogInformation("Finsihed loading crunchyroll anime");
		}

		public async Task Hidive()
		{
			var data = await _hidive.Fetch("https://www.hidive.com/movies/", "movie").ToArrayAsync();
			//using var io = File.OpenWrite("hidive.json");
			//await JsonSerializer.SerializeAsync(io, data);
			await _mongo.Upsert(data);
		}

		public async Task Test()
		{
			await new[] { 47, 50, 49 }
				.Select(t => _lnDb.Series.Delete(t))
				.WhenAll();
		}

		public async Task Load()
		{
			using var io = File.OpenRead("hidive.json");
			var data = await JsonSerializer.DeserializeAsync<Anime[]>(io);

			_logger.LogInformation("File Loaded");

			if (data == null)
			{
				_logger.LogError("Data is null");
				return;
			}

			foreach (var item in data.Clean())
				item.Id = null;

			_logger.LogInformation("Ids nulled");

			await _mongo.Upsert(data);

			_logger.LogInformation("Data loaded");
		}

		public async Task FetchFunimationResources()
		{
			var data = await _fun.All().ToListAsync();
			using var io = File.OpenWrite(FUN_JSON);
			await JsonSerializer.SerializeAsync(io, data);
		}

		public async Task DeteremineImageSizes()
		{
			var dic = new Dictionary<string, List<(int width, int height, string source)>>();
			var data = await _fun.All().ToListAsync();

			for(var i = 0; i < data.Count && i < 5; i++)
			{
				var cur = data[i];
				foreach (var im in cur.Images)
				{
					if (string.IsNullOrEmpty(im.Source))
					{
						_logger.LogInformation($"Skipping \"{im.PlatformId}\" for \"{cur.Title}\" as it's source is empty");
						continue;
					}

					using var res = await _api.Create(im.Source).Result();
					if (res == null || !res.IsSuccessStatusCode)
					{
						_logger.LogError("Failed to fetch resource: " + im.Source);
						continue;
					}

					using var io = await res.Content.ReadAsStreamAsync();
					using var image = await SLImage.LoadAsync(io);

					if (!dic.ContainsKey(im.PlatformId)) dic.Add(im.PlatformId, new());

					dic[im.PlatformId].Add((image.Width, image.Height, im.Source));
				}
			}

			_logger.LogInformation("Results:");
			foreach(var (type, sizes) in dic)
			{
				_logger.LogInformation(type);
				foreach (var (w, h, _) in sizes)
					_logger.LogInformation($"\t{w} x {h}");
			}
		}

		public async Task All()
		{
			var services = new IAnimeApiService[] { _vrv, _fun };

			var tasks = services.Select(t => t.All().ToListAsync().AsTask());
			var data = (await Task.WhenAll(tasks)).SelectMany(t => t).ToArray();
			if (data == null)
			{
				_logger.LogError("Data returned is null");
				return;
			}

			using var io = File.OpenWrite("all.json");
			await JsonSerializer.SerializeAsync(io, data);
		}

		public async Task FormatVrvResources()
		{
			var data = await _vrv.All().ToListAsync();
			using var io = File.OpenWrite(VRV_JSON);
			await JsonSerializer.SerializeAsync(io, data);
		}

		public async Task ReformatIds()
		{
			const string PATH = "all.json";
			using var i = File.OpenRead(PATH);
			var data = await JsonSerializer.DeserializeAsync<Anime[]>(i);
			await i.DisposeAsync();

			if (data == null)
			{
				_logger.LogError("Data is null");
				return;
			}

			foreach (var anime in data)
				anime.HashId = $"{anime.PlatformId}-{anime.AnimeId}-{anime.Title}".MD5Hash();

			File.Delete(PATH);

			using var o = File.OpenWrite(PATH);
			await JsonSerializer.SerializeAsync(o, data);
		}

		public async Task FetchVrvResources()
		{
			var output = new List<VrvResourceResult>();

			var ops = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
			foreach (var op in ops)
			{
				var resources = await _vrv.Fetch(op.ToString());
				if (resources == null)
				{
					_logger.LogWarning("Resource not found for: " + op);
					continue;
				}
				_logger.LogInformation($"{resources.Total} found for: {op}");
				output.Add(resources);
			}

			using var io = File.OpenWrite(VRV_JSON);
			await JsonSerializer.SerializeAsync(io, output);

			_logger.LogInformation("Finished writing");
		}

		public async Task Clean()
		{
			var data = await _db.All();
			if (data == null)
			{
				_logger.LogError("Data is null");
				return;
			}

			foreach (var anime in data)
				await _db.Upsert(anime.Clean());
		}

		public async Task Migrate()
		{
			var convertImage = (AImage i) =>
			{
				return new DbImage
				{
					Width = i.Width,
					Height = i.Height,
					PlatformId = i.PlatformId,
					Source = i.Source,
					Type = i.Type,
				};
			};

			var convertAnime = (Anime a) =>
			{
				return new DbAnime
				{
					HashId = a.HashId,
					AnimeId = a.AnimeId,
					Link = a.Link,
					Title = a.Title,
					Description = a.Description,
					PlatformId = a.PlatformId,
					Type = a.Type,
					Mature = a.Metadata.Mature,
					Languages = a.Metadata.Languages.ToArray(),
					LanguageTypes = a.Metadata.LanguageTypes.ToArray(),
					Ratings = a.Metadata.Ratings.ToArray(),
					Tags = a.Metadata.Tags.ToArray(),
					Images = a.Images.Select(convertImage).ToArray(),
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now
				};
			};

			var all = await _mongo.All(1, 9000);

			foreach(var a in all.Results)
				await _db.Upsert(convertAnime(a));
		}

		public async Task LoadLightNovel()
		{
			const string FIRST_CHAPTER = "";
			const int SRC = 1;

			var src = _ln.Sources()[SRC];

			var chaps = src.DbChapters(FIRST_CHAPTER);

			if (chaps == null)
			{
				_logger.LogError("No chapters found to load!");
				return;
			}

			await foreach (var chap in chaps)
				await _chapDb.Upsert(chap);

			_logger.LogInformation("Book uploaded!");
		}

		public async Task ToPdf()
		{
			const string ID = "445C5E7AC91435D2155BC1D1DAAE8EB8";
			await _pdf.ToPdf(ID);
		}

		public async Task ToEpub()
		{
			const string JM_IMG_DIR = @"C:\Users\Cardboard\Desktop\JM";
			var cvi = (int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Cover.jpg";
			var coi = (int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Contents.jpg";
			var ini = (int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Inserts";
			var frd = (int volume) => $"{JM_IMG_DIR}\\Vol{volume}\\Forwards";

			var genSet = (int index, int start, int end) =>
			{
				var vol = index + 1;
				var toUris = (string dir) => Directory.GetFiles(dir).Select(t => "file://" + t).ToArray();

				var forwards = toUris(frd(vol));
				var contents = coi(vol);
				if (File.Exists(contents))
					forwards = forwards.Append("file://" + contents).ToArray();

				return new EpubSettings
				{
					Start = start,
					Stop = end,
					Vol = vol,
					Translator = "Supreme Tentacle",
					Editor = "Joker",
					Author = "Ryuyu",
					Publisher = "Cardboard Box",
					Illustrator = "Dabu Ryu",
					CoverUrl = "file://" + cvi(vol),
					ForwardUrls = forwards,
					InsertUrls = toUris(ini(vol))
				};
			};

			const string BOOK_ID = "445C5E7AC91435D2155BC1D1DAAE8EB8";
			var ranges = new[]
			{
				(1, 42),
				(43, 88),
				(89, 122),
				(123, 158),
				(159, 185),
				(186, 224),
				(225, 266),
				(267, 290),
				(291, 324),
				(325, 358),
				(359, 391),
				(391, 422)
			};

			var settings = ranges.Select((t, i) => genSet(i, t.Item1, t.Item2)).ToArray();
			var (epubs, dir) = await _ln.GenerateEpubs(BOOK_ID, settings);

			foreach (var epub in epubs)
			{
				var name = Path.GetFileName(epub);
				if (File.Exists(name)) File.Delete(name);
				File.Move(epub, name);
			}

			new DirectoryInfo(dir).Delete(true);
		}

		public async Task PickupNew()
		{
			const string ID = "445C5E7AC91435D2155BC1D1DAAE8EB8";
			const int SOURCE_ID = 0;
			var src = _ln.Sources()[SOURCE_ID];
			var book = await _chapDb.BookById(ID);
			if (book == null)
			{
				_logger.LogWarning($"Could not find book with ID: {ID}");
				return;
			}

			var cur = src.DbChapters(book.LastChapterUrl);

			int count = 0;
			await foreach(var item in cur)
			{
				item.Ordinal = book.LastChapterOrdinal + count;
				await _chapDb.Upsert(item);
				count++;
			}

			if (count == 1)
			{
				_logger.LogInformation($"No new chapters for: {book.Title}");
				return;
			}

			_logger.LogInformation($"New chapters loaded: {book.Title} - {count - 1}");
		}

		public async Task MigrateBooks()
		{
			var (_, books) = await _chapDb.Books();

			await Task.WhenAll(books.Select(MigrateBook));
		}

		public async Task MigrateBook(DbBook book)
		{
			try
			{
				var src = _ln.Source(book.LastChapterUrl);
				if (src == null)
				{
					_logger.LogWarning($"Could not find source for: {book.Title} - {book.LastChapterUrl}");
					return;
				}

				var seriesUrl = src.SeriesFromChapter(book.LastChapterUrl);
				var data = await src.GetSeriesInfo(seriesUrl);

				if (data == null)
				{
					_logger.LogWarning($"Could not find series data for: {book.Title} - {book.LastChapterUrl}");
					return;
				}

				var id = await _lnDb.Series.Upsert(new Series
				{
					HashId = book.Title.MD5Hash(),
					Title = book.Title,
					Url = seriesUrl,
					LastChapterUrl = book.LastChapterUrl,
					Image = data.Image,
					Genre = data.Genre,
					Tags = data.Tags,
					Authors = data.Authors,
					Illustrators = Array.Empty<string>(),
					Editors = Array.Empty<string>(),
					Translators = Array.Empty<string>(),
					Description = data.Description
				});
				var (_, chaps) = await _chapDb.Chapters(book.Id, 1, 10000);

				foreach (var chap in chaps)
				{
					await _lnDb.Pages.Upsert(new Page
					{
						SeriesId = id,
						HashId = chap.HashId,
						Url = chap.Url,
						NextUrl = string.IsNullOrEmpty(chap.NextUrl) ? null : chap.NextUrl,
						Content = chap.Content,
						Mimetype = "application/html",
						Title = chap.Chapter,
						Ordinal = chap.Ordinal
					});
				}

				_logger.LogInformation($"Finished migrating: {book.Title}::{id} - {chaps.Length} pages");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error occurred while processing: {book.Title}");
				throw;
			}
		}

		public async Task DoJm()
		{
			const long SERIES_ID = 8;
			var scaffold = await _lnDb.Series.Scaffold(SERIES_ID);

			const string JM_IMG_DIR = @"C:\Users\Cardboard\Desktop\JM";
			const string JM_IMG_URL = "https://static.index-0.com/jm/volumeart";
			var cvi = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Cover.jpg", $"{JM_IMG_URL}/Vol{volume}/Cover.jpg");
			var coi = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Contents.jpg", $"{JM_IMG_URL}/Vol{volume}/Contents.jpg");
			var ini = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Inserts", $"{JM_IMG_URL}/Vol{volume}/Inserts");
			var frd = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Forwards", $"{JM_IMG_URL}/Vol{volume}/Forwards");
			var toUris = ((string dir, string url) a) => Directory.GetFiles(a.dir).Select(t => $"{a.url}/{Path.GetFileName(t)}").ToArray();

			
			var series = await _lnDb.Series.Fetch(SERIES_ID);
			var (_, _, pages) = await _lnDb.Pages.Paginate(SERIES_ID, 1, 1000);

			var ranges = new[]
			{
				(1, 42),
				(43, 88),
				(89, 122),
				(123, 158),
				(159, 185),
				(186, 224),
				(225, 266),
				(267, 290),
				(291, 324),
				(325, 358),
				(359, 391),
				(391, (int?)null)
			};
			var doChunks = (Page[] pages, (int start, int? stop)[] ranges) =>
			{
				var output = new List<Page[]>();
				int r = 0;
				var cur = new List<Page>();

				for (var i = 0; i < pages.Length; i++)
				{
					if (r >= ranges.Length) break;

					var page = pages[i];
					var (start, stop) = ranges[r];
					if (i + 1 < start || (stop != null && i + 1 > stop))
					{
						r++;
						output.Add(cur.ToArray());
						cur.Clear();
					}

					cur.Add(page);
				}

				if (cur.Count > 0)
					output.Add(cur.ToArray());

				return output.ToArray();
			};

			var chunks = doChunks(pages, ranges);

			if (chunks.Length != ranges.Length)
			{
				_logger.LogWarning($"Chunks ({chunks.Length}) / Ranges Mismatch ({ranges.Length})");
				return;
			}

			series.Illustrators = new[] { "Dabu Ryu" };
			series.Translators = new[] { "Supreme Tentacle", "Yuuki" };
			series.Editors = new[] { "Joker", "SpeedPheonix" };

			await _lnDb.Series.Upsert(series);

			for (var i = 0; i < ranges.Length; i++)
			{
				var (start, stop) = ranges[i];
				var vol = i + 1;
				var forwards = toUris(frd(vol));
				var inserts = toUris(ini(vol));
				var (cdir, curl) = coi(vol);
				if (File.Exists(cdir))
					forwards = forwards.Append($"{curl}/{Path.GetFileName(cdir)}").ToArray();

				var title = $"{series.Title} Vol {vol}";

				var id = await _lnDb.Books.Upsert(new Book
				{
					SeriesId = SERIES_ID,
					CoverImage = cvi(vol).Item2,
					Forwards = forwards,
					Inserts = inserts,
					Title = title,
					HashId = title.MD5Hash(),
					Ordinal = vol
				});

				var pageChunk = chunks[i];
				for(var p = 0; p < pageChunk.Length; p++)
				{
					var page = pageChunk[p];
					var chapId = await _lnDb.Chapters.Upsert(new Chapter
					{
						HashId = $"{page.Title}-{i}-{p}".MD5Hash(),
						Title = page.Title,
						Ordinal = p,
						BookId = id
					});
					await _lnDb.ChapterPages.Upsert(new ChapterPage
					{
						ChapterId = chapId,
						PageId = page.Id,
						Ordinal = 0
					});
				}
			}
		}

		public async Task TestMangaDex()
		{
			var item = await _mangaDex.Search("sonna hiroki");

			Console.WriteLine($"Manga: {item?.Data?.Count}");
		}

		public async Task TestMangaClash()
		{
			const string id = "402922",
				chapId = "chapter-118";

			var manga = await _nhentai.Manga(id);
			var chap = await _nhentai.ChapterPages(id, chapId);

			Console.WriteLine("Manga Source: " + manga?.Title);
		}
	}
}
