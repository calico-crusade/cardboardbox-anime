using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace CardboardBox.LightNovel.Core;

using Anime.Core;
using Epub;
using ImageTransformers;

public interface INovelEpubService
{
	Task<StreamResult?> Generate(params long[] bookIds);
}

public class NovelEpubService : INovelEpubService
{
	private const string EPUB_MIMETYPE = "application/epub+zip";

	private readonly ILnDbService _db;
	private readonly ILogger _logger;
	private readonly IApiService _api;
	private readonly IFileCacheService _file;

	public NovelEpubService(
		ILnDbService db, 
		ILogger<NovelEpubService> logger,
		IApiService api,
		IFileCacheService file)
	{
		_db = db;
		_logger = logger;
		_api = api;
		_file = file;
	}

	public async Task<StreamResult?> Generate(params long[] bookIds)
	{
		if (bookIds.Length == 0) throw new ArgumentException("Please specify at least 1 book to generate", nameof(bookIds));
		if (bookIds.Length == 1) return await GenerateOneBook(bookIds[0]);

		var pubs = (await bookIds.Select(GenerateRawBook).WhenAll())
			.Where(t => t != null)
			.Select(t => t ?? ("", ""))
			.ToArray();

		if (pubs.Length == 0) return null;

		var io = new MemoryStream();
		using (var o = new ZipOutputStream(io))
		{
			o.IsStreamOwner = false;

			foreach (var (path, name) in pubs)
			{
				if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name)) continue;

				await o.PutNextEntryAsync(new ZipEntry(name));
				using (var f = File.OpenRead(path))
					StreamUtils.Copy(f, o, new byte[4096]);
				await o.CloseEntryAsync(CancellationToken.None);
			}
		}

		pubs.Each(t => File.Delete(t.path));

		io.Position = 0;
		return  new StreamResult(io, "epubs.zip", "application/zip");
	}

	public async Task<StreamResult?> GenerateOneBook(long bookId)
	{
		var (path, name) = (await GenerateRawBook(bookId)) ?? ("", "");
		if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(name)) return null;

		var io = new MemoryStream();
		using var f = File.OpenRead(path);
		await f.CopyToAsync(io);

		io.Position = 0;
		return new StreamResult(io, name, EPUB_MIMETYPE);
	}

	public async Task<(string path, string name)?> GenerateRawBook(long bookId)
	{
		var ooo = (string[] first, string[] second) => first.Union(second).ToArray();

		var scaffold = await _db.Books.Scaffold(bookId);
		if (scaffold == null) return null;

		var (series, book, chaps) = scaffold;

		var path = Path.GetTempFileName();

		await using (var epub = EpubBuilder.Create(book.Title, path))
		{
			var bob = await epub.Start();

			bob.BelongsTo(series.Title, (int)book.Ordinal);

			ooo(series.Authors, book.Authors).Each(t => bob.Author(t));
			ooo(series.Illustrators, book.Illustrators).Each(t => bob.Illustrator(t));
			ooo(series.Editors, book.Editors).Each(t => bob.Editor(t));
			ooo(series.Translators, book.Translators).Each(t => bob.Translator(t));

			await HandleCoverImage(bob, book);
			await bob.AddStylesheetFromFile("stylesheet.css", "stylesheet.css");
			await HandleForwards(bob, book);
			await HandleChapters(bob, book, chaps);
			await HandleInserts(bob, book);
		}

		return (path, $"{book.Title}.epub".PurgePathChars());
	}

	#region Epub Builder Helpers

	public async Task HandleCoverImage(IEpubBuilder bob, Book book)
	{
		if (string.IsNullOrEmpty(book.CoverImage)) return;

		var (data, file, type) = await GetData(book.CoverImage);
		var name = DetermineName(book.CoverImage, file, type);
		await bob.AddCoverImage(name, data);
		await data.DisposeAsync();
	}

	public async Task HandleImageChapter(IEpubBuilder bob, string[] urls, string title)
	{
		if (!urls.Any()) return;

		await bob.AddChapter(title, async c =>
		{
			foreach (var image in urls)
			{
				var (dataIn, file, type) = await GetData(image);
				var name = DetermineName(image, file, type);
				using var data = dataIn;
				await c.AddImage(name, data);
			}
		});
	}

	public Task HandleForwards(IEpubBuilder bob, Book book) => HandleImageChapter(bob, book.Forwards, "Illustrations");

	public Task HandleInserts(IEpubBuilder bob, Book book) => HandleImageChapter(bob, book.Inserts, "Inserts");

	public async Task HandleChapters(IEpubBuilder bob, Book book, ChapterScaffold[] chapters)
	{
		for(var i = 0; i < chapters.Length; i++)
		{
			var (chap, pages) = chapters[i];
			await bob.AddChapter(chap.Title, async c =>
			{
				for(var p = 0; p < pages.Length; p++)
				{
					var (page, _) = pages[p];

					if (page.Mimetype.ToLower() == "application/html")
					{
						var header = p == 0 ? $"<h1>{chap.Title}</h1>" : "";
						var content = $"{header}{CleanContents(page.Content, page.Title)}";
						await PostFixImages(bob, c, $"chapter-{i}-{p}.xhtml", content);
						continue;
					}

					if (page.Mimetype.ToLower().StartsWith("image/"))
					{
						if (p == 0)
							await c.AddRawPage($"chapter-{i}-{p}.xhtml", $"<h1>{chap.Title}</h1>");

						var (dataIn, file, type) = await GetData(page.Content);
						var name = DetermineName(page.Content, file, type);
						await c.AddImage(name, dataIn);
						await dataIn.DisposeAsync();
						continue;
					}

					_logger.LogWarning($"Unknown Mimetype for: [Book:{book.Id}]::[Page:{page.Id}] - {page.Mimetype}");
				}
			});
		}
	}

	public async Task PostFixImages(IEpubBuilder epub, IChapterBuilder bob, string filename, string content)
	{
		if (!content.ToLower().Contains("<img"))
		{
			await bob.AddRawPage(filename, content);
			return;
		}

		var doc = new HtmlDocument();
		doc.LoadHtml(content);

		foreach(var img in doc.DocumentNode.SelectNodes("//img"))
		{
			var src = img.GetAttributeValue("src", "");
			if (string.IsNullOrEmpty(src))
			{
				img.Remove();
				continue;
			}

			try
			{
				var (dataIn, file, type) = await GetData(src);
				var name = DetermineName(src, file, type);
				var relPath = await epub.AddFile(name, dataIn, FileType.Image);
				img.SetAttributeValue("src", relPath);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error occurred while attempting to load image: \"{src}\" Into \"{filename}\"");
				img.Remove();
			}
		}

		content = doc.DocumentNode.InnerHtml;
		await bob.AddRawPage(filename, content);
	}

	#endregion

	#region Utilities

	public string RandomBits(int size, string? chars = null)
	{
		chars ??= "abcdefghijklmnopqrstuvwxyz0123456789";
		var r = new Random();

		var output = "";
		for (var i = 0; i < size; i++)
			output += chars[r.Next(0, chars.Length)];

		return output;
	}

	public string GetImageExtension(string type)
	{
		return type switch
		{
			"image/jpeg" => "jpg",
			"image/jpg" => "jpg",
			"image/png" => "png",
			"image/webp" => "webp",
			_ => throw new NotSupportedException($"`{type}` is not a known media-type")
		};
	}

	public string DetermineName(string url, string name, string type)
	{
		if (!string.IsNullOrEmpty(name) && 
			string.IsNullOrEmpty(Path.GetExtension(name)))
			name += "." + GetImageExtension(type);

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

	public async Task<StreamResult> GetData(string url, bool skipTransform = false)
	{
		if (url.ToLower().StartsWith("https://static.index-0.com/"))
			url = url.Replace("https://static.index-0.com/", "file://C:/users/cardboard/documents/local-files/");

		var output = await (url.ToLower().StartsWith("file://") ? GetDataFromFile(url.Remove(0, 7)) : _file.GetFile(url));

		if (output.Mimetype != "image/webp" || skipTransform) return output;

		var pngStream = new MemoryStream();
		await output.Stream.ConvertImage(pngStream, ImageMagick.MagickFormat.Png);
		pngStream.Position = 0;

		return new StreamResult(pngStream, $"{Path.GetFileNameWithoutExtension(output.Name)}.png", "image/png");
	}

	public Task<StreamResult> GetDataFromFile(string path)
	{
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
		return Task.FromResult(new StreamResult(stream, name, type));
	}

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

	#endregion
}

