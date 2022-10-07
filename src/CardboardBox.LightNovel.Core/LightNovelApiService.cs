using System.Text.RegularExpressions;

namespace CardboardBox.LightNovel.Core
{
	using Anime.Database;
	using Epub;
	using Sources;

	public interface ILightNovelApiService
	{
		Task<Chapter[]?> FromJson(string path);
		Task<(string[] files, string wrkDir)> GenerateEpubs(string bookId, EpubSettings[] settings, string? workDir = null);
		ISourceService[] Sources();
		ISourceService? Source(string url);

		Task<(string? id, int? added)> LoadFromBookId(string bookId);
		Task<(string? id, bool isNew, int? added)> LoadFromUrl(string url);
	}

	public class LightNovelApiService : ILightNovelApiService
	{
		private readonly ILnpSourceService _src1;
		private readonly IShSourceService _src2;
		private readonly IChapterDbService _db;
		private readonly IApiService _api;
		private readonly ILogger _logger;

		public LightNovelApiService(
			ILnpSourceService src1,
			IShSourceService src2,
			IChapterDbService db,
			IApiService api,
			ILogger<LightNovelApiService> logger)
		{
			_src1 = src1;
			_src2 = src2;
			_db = db;
			_api = api;
			_logger = logger;
		}

		public async Task<Chapter[]?> FromJson(string path)
		{
			using var io = File.OpenRead(path);
			return await JsonSerializer.DeserializeAsync<Chapter[]>(io).AsTask();
		}

		public ISourceService[] Sources() => new[] { (ISourceService)_src1, _src2 };

		public ISourceService? Source(string url)
		{
			var root = url.GetRootUrl().ToLower();

			return Sources()
				.Where(t => t.RootUrl.ToLower() == root)
				.FirstOrDefault();
		}

		public async Task<(string? id, bool isNew, int? added)> LoadFromUrl(string url)
		{
			var src = Source(url);
			if (src == null) return (null, true, -1);

			var book = await _db.BookByUrl(url);

			if (book == null)
			{
				var (id, added) = await LoadNewBook(src, url);
				return (id, true, added);
			}

			var newChaps = await PickupNewChapters(src, book);
			return (book.Id, false, newChaps);
		}

		public async Task<(string? id, int? added)> LoadFromBookId(string bookId)
		{
			var book = await _db.BookById(bookId);

			if (book == null) return (null, 0);

			var src = Source(book.LastChapterUrl);
			if (src == null) return (null, -1);

			var newChaps = await PickupNewChapters(src, book);
			return (book.Id, newChaps);
		}

		public async Task<(string? bookId, int? added)> LoadNewBook(ISourceService source, string url)
		{
			var chaps = source.DbChapters(url);
			DbChapter? last = null;
			int count = 0;
			await foreach (var chap in chaps)
			{
				if (count == 0 && string.IsNullOrWhiteSpace(chap.Book)) return (null, 0);
				await _db.Upsert(last = chap);
				count++;
			}
			return (last?.BookId, count);
		}

		public async Task<int> PickupNewChapters(ISourceService src, DbBook book)
		{
			var newChaps = src.DbChapters(book.LastChapterUrl);
			int count = 0;
			await foreach(var item in newChaps)
			{
				item.Ordinal = book.LastChapterOrdinal + count;
				await _db.Upsert(item);
				count++;
			}

			return count;
		}

		public IEnumerable<(DbChapter[] chunk, EpubSettings settings)> Chunks(DbChapter[] chaps, EpubSettings[] settings)
		{
			int r = 0;
			var cur = new List<DbChapter>();

			for (var i = 0; i < chaps.Length; i++)
			{
				if (r >= settings.Length) yield break;

				var set = settings[r];
				int start = set.Start,
					stop = set.Stop;

				if (i + 1 < start || i + 1 > stop)
				{
					r++;
					yield return (cur.ToArray(), set);
					cur.Clear();
				}

				cur.Add(chaps[i]);
			}

			if (cur.Count > 0)
				yield return (cur.ToArray(), settings.Last());
		}

		public async Task<(string[] files, string wrkDir)> GenerateEpubs(string bookId, EpubSettings[] settings, string? workDir = null)
		{
			workDir ??= Path.GetTempPath();
			var dir = Path.Combine(workDir, "cba-epub-host-" + Guid.NewGuid().ToString());

			if (settings.All(t => t.SkipGeneration)) return (Array.Empty<string>(), dir);

			var (_, chaps) = await _db.Chapters(bookId, 1, 9999);
			if (chaps.Length == 0) return (Array.Empty<string>(), dir);

			var chunks = Chunks(chaps, settings.OrderBy(t => t.Start).ToArray());

			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

			var tasks = chunks
				.Where(t => !t.settings.SkipGeneration)
				.Select(t => Volume(t.chunk, t.settings, dir));
			return (await Task.WhenAll(tasks), dir);
		}

		public async Task<string> Volume(DbChapter[] chaps, EpubSettings settings, string dir)
		{
			var seriesTitle = chaps.First().Book;
			var title = $"{seriesTitle} Vol {settings.Vol}";
			var path = Path.Combine(dir, $"{title.SnakeName()}.epub");
			var wrk = Path.Combine(dir, $"vol-{settings.Vol}");

			_logger.LogInformation($"Processing Novel Request: {title} ({settings.Start} - {settings.Stop}) - Output: {path}");

			await using var epub = EpubBuilder.Create(title, path, null, wrk);
			var bob = await epub.Start();

			bob.BelongsTo(seriesTitle, settings.Vol);
			if (!string.IsNullOrEmpty(settings.Author)) bob.Author(settings.Author);
			if (!string.IsNullOrEmpty(settings.Publisher)) bob.Publisher(settings.Publisher);
			if (!string.IsNullOrEmpty(settings.Editor)) bob.Editor(settings.Editor);
			if (!string.IsNullOrEmpty(settings.Illustrator)) bob.Illustrator(settings.Illustrator);
			if (!string.IsNullOrEmpty(settings.Translator)) bob.Translator(settings.Translator);

			await HandleCoverImage(bob, settings);
			await HandleStyleSheet(bob, settings);
			await HandleForwards(bob, settings);
			await HandleChapter(bob, chaps);
			await HandleInserts(bob, settings);
			return path;
		}

		public async Task HandleChapter(IEpubBuilder bob, DbChapter[] chaps)
		{
			for(var i = 0; i < chaps.Length; i++)
			{
				var chap = chaps[i];
				await bob.AddChapter(chap.Chapter, async c =>
				{
					await c.AddRawPage($"chapter{i}.xhtml", $"<h1>{chap.Chapter}</h1>{CleanContents(chap.Content, chap.Chapter)}");
				});
			}
		}

		public string RandomBits(int size, string? chars = null)
		{
			chars ??= "abcdefghijklmnopqrstuvwxyz0123456789";
			var r = new Random();

			var output = "";
			for (var i = 0; i < size; i++)
				output += chars[r.Next(0, chars.Length)];

			return output;
		}

		public string DetermineName(string url, string name, string type)
		{
			if (!string.IsNullOrEmpty(name)) return RandomBits(5) + name;

			name = url.Split('/').Last();
			var ext = Path.GetExtension(name);
			if (!string.IsNullOrEmpty(ext)) return RandomBits(5) + name;

			ext = type switch
			{
				"image/jpeg" => "jpg",
				"image/png" => "png",
				"image/webp" => "webp",
				"text/css" => "css",
				_ => throw new NotSupportedException($"`{type}` is not a known media-type")
			};

			return Path.GetRandomFileName() + "." + ext;
		}

		public async Task HandleStyleSheet(IEpubBuilder bob, EpubSettings settings)
		{
			if (string.IsNullOrEmpty(settings.StylesheetUrl))
			{
				await bob.AddStylesheetFromFile("stylesheet.css", "stylesheet.css");
				return;
			}

			var (dataIn, _, _, _) = await GetData(settings.StylesheetUrl);
			using var data = dataIn;
			await bob.AddStylesheet("stylesheet.css", data);
		}

		public async Task HandleImageChapter(IEpubBuilder bob, string[] urls, string title)
		{
			if (!urls.Any()) return;

			await bob.AddChapter(title, async c =>
			{
				foreach (var image in urls)
				{
					var (dataIn, _, file, type) = await GetData(image);
					var name = DetermineName(image, file, type);
					using var data = dataIn;
					await c.AddImage(name, data);
				}
			});
		}

		public async Task HandleCoverImage(IEpubBuilder bob, EpubSettings settings)
		{
			if (string.IsNullOrEmpty(settings.CoverUrl)) return;

			var (dataIn, _, file, type) = await GetData(settings.CoverUrl);
			var name = DetermineName(settings.CoverUrl, file, type);
			await bob.AddCoverImage(name, dataIn);
		}

		public Task HandleForwards(IEpubBuilder bob, EpubSettings settings) => HandleImageChapter(bob, settings.ForwardUrls, "Illustrations");

		public Task HandleInserts(IEpubBuilder bob, EpubSettings settings) => HandleImageChapter(bob, settings.InsertUrls, "Inserts");

		public string CleanContents(string content, string chap)
		{
			var pats = new[] 
			{ 
				" class=\"(.*?)\"",
				"<a (.*?)>(.*?)</a>",
				" style=\"(.*?)\""
			};

			foreach (var p in pats)
				content = Regex.Replace(content, p, "");

			try
			{
				//This is purely to fix some malformed data within the files
				content = content
					.Replace("<hr>", "")
					.Replace("<hr/>", "")
					.Replace("<hr />", "")
					.Replace("<br>", "</p><p>");

				content = FixMissingTags(content, "p");
				content = FixMissingTags(content, "span");
				
				return content;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error occurred while processing chapter: " + chap);
				return content;
			}
		}

		public string FixMissingTags(string content, string tag)
		{
			string start = $"<{tag}>",
				stop = $"</{tag}";

			int i = 0;
			while (i < content.Length)
			{
				var fis = content.IndexOf(start, i);
				if (fis == -1) break;

				var nis = content.IndexOf(start, fis + start.Length);
				var fie = content.IndexOf(stop, fis);

				if (nis == -1) break;

				if (nis < fie)
					content = content.Insert(nis, stop);

				if (fie == -1)
				{
					content += stop;
					break;
				}

				i = fie + 1;
			}
			return content;
		}

		public Task<(Stream stream, long length, string filename, string type)> GetData(string url)
		{
			if (url.ToLower().StartsWith("file://")) return GetDataFromFile(url.Remove(0, 7));
			return _api.GetData(url);
		}

		public Task<(Stream stream, long length, string filename, string type)> GetDataFromFile(string path)
		{
			var fileInfo = new FileInfo(path);
			var name = Path.GetFileName(path);
			var ext = Path.GetExtension(path).ToLower().TrimStart('.');
			var type = ext switch
			{
				"jpg" => "image/jpeg",
				"jpeg" => "image/jpeg",
				"png" => "image/png",
				"css" => "text/css",
				"html" => "text/html",
				"webp" => "image/webp",
				_ => throw new NotSupportedException($"File Extension \"{ext}\" mime-type is unknown")
			};

			var stream = (Stream)File.OpenRead(path);
			return Task.FromResult((stream, fileInfo.Length, name, type));
		}
	}
}
