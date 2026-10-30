using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

using System.Collections.Concurrent;
using CardboardBox.Extensions;

namespace CardboardBox.LightNovel.Core;

using Anime.Core;
using Epub;
using ImageTransformers;

public interface INovelEpubService
{
	Task<StreamResult?> Generate(params long[] bookIds);
}

public class NovelEpubService(
    ILnDbService _db,
    ILogger<NovelEpubService> _logger,
    IFileCacheService _file) : INovelEpubService
{
	private const string EPUB_MIMETYPE = "application/epub+zip";
	private const int EPUB_WORDS_PER_PAGE = 350;

	private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileDownloads = [];

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
			_logger.LogDebug("Starting EPUB generation for [Book:{bookId}]::\"{bookTitle}\"", book.Id, book.Title);
            var bob = await epub.Start();
			bob.EnableApproximatePageBreaks(EPUB_WORDS_PER_PAGE);

			bob.BelongsTo(series.Title, (int)book.Ordinal);

			ooo(series.Authors, book.Authors).Each(t => bob.Author(t));
			ooo(series.Illustrators, book.Illustrators).Each(t => bob.Illustrator(t));
			ooo(series.Editors, book.Editors).Each(t => bob.Editor(t));
			ooo(series.Translators, book.Translators).Each(t => bob.Translator(t));

			_logger.LogDebug("Handling cover image for [Book:{bookId}]::\"{bookTitle}\"", book.Id, book.Title);
            await HandleCoverImage(bob, book, series.Url);

            if (!string.IsNullOrWhiteSpace(book.Styles))
				await bob.AddStylesheet("custom.css", book.Styles);
            else
                await bob.AddStylesheetFromFile("stylesheet.css", "stylesheet.css");

			_logger.LogDebug("Handling forwards for [Book:{bookId}]::\"{bookTitle}\"", book.Id, book.Title);
            await HandleForwards(bob, book, series.Url);
			_logger.LogDebug("Handling chapters for [Book:{bookId}]::\"{bookTitle}\"", book.Id, book.Title);
            await HandleChapters(bob, book, chaps);
			_logger.LogDebug("Handling inserts for [Book:{bookId}]::\"{bookTitle}\"", book.Id, book.Title);
            await HandleInserts(bob, book, series.Url);
			_logger.LogDebug("Finalizing EPUB generation for [Book:{bookId}]::\"{bookTitle}\"", book.Id, book.Title);
        }

		return (path, FormatFileName(scaffold));
	}

	#region Epub Builder Helpers

	public async Task HandleCoverImage(IEpubBuilder bob, Book book, string? sourceUrl = null)
	{
		if (string.IsNullOrEmpty(book.CoverImage) || IsPlaceholderImage(book.CoverImage)) return;

		var url = ResolveUrl(book.CoverImage, sourceUrl);
		var (data, file, type) = await GetData(url);
		var name = DetermineName(url, file, type);
		await bob.AddCoverImage(name, data);
		await data.DisposeAsync();
	}

	public async Task HandleImageChapter(IEpubBuilder bob, string[] urls, string title, string? sourceUrl = null)
	{
		var images = urls.Where(t => !IsPlaceholderImage(t)).ToArray();
		if (images.Length == 0) return;

		await bob.AddChapter(title, async c =>
		{
			foreach (var image in images)
			{
				var url = ResolveUrl(image, sourceUrl);
				var (dataIn, file, type) = await GetData(url);
				var name = DetermineName(url, file, type);
				using var data = dataIn;
				await c.AddImage(name, data);
			}
		});
	}

	public Task HandleForwards(IEpubBuilder bob, Book book, string? sourceUrl = null) => HandleImageChapter(bob, book.Forwards, "Illustrations", sourceUrl);

	public Task HandleInserts(IEpubBuilder bob, Book book, string? sourceUrl = null) => HandleImageChapter(bob, book.Inserts, "Inserts", sourceUrl);

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

					if (page.Mimetype.EqualsIc("application/html"))
					{
						var clean = CleanContents(page.Content, page.Title);
						if (p == 0 && !clean.ContainsIc(page.Title))
							clean = $"<h1>{chap.Title}</h1>{clean}";
						await PostFixImages(bob, c, $"chapter-{i}-{p}.xhtml", clean, page.Url);
						continue;
					}

					if (page.Mimetype.StartsWithIc("image/"))
					{
						if (p == 0)
							await c.AddRawPage($"chapter-{i}-{p}.xhtml", $"<h1>{chap.Title}</h1>");

						var url = ResolveUrl(page.Content, page.Url);
						var (dataIn, file, type) = await GetData(url);
						var name = DetermineName(url, file, type);
						await c.AddImage(name, dataIn);
						await dataIn.DisposeAsync();
						continue;
					}

					_logger.LogWarning("Unknown Mimetype for: [Book:{Id}]::[Page:{pageId}] - {Mimetype}", book.Id, page.Id, page.Mimetype);
				}
			});
		}
	}

	public async Task PostFixImages(IEpubBuilder epub, IChapterBuilder bob, string filename, string content, string? sourceUrl = null)
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
				var url = ResolveUrl(src, sourceUrl);
				var (dataIn, file, type) = await GetData(url);
				using var data = dataIn;
				var name = DetermineName(url, file, type);
				var relPath = await epub.AddFile(name, data, FileType.Image);
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

	public static bool IsPlaceholderImage(string url)
	{
		var path = Uri.TryCreate(url, UriKind.Absolute, out var uri)
			? uri.AbsolutePath
			: url.Split('?', '#')[0];

		return Path.GetFileName(path).Equals("nocover-new-min.png", StringComparison.OrdinalIgnoreCase);
	}

	public static string ResolveUrl(string url, string? sourceUrl)
	{
		if (string.IsNullOrWhiteSpace(sourceUrl) ||
			url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
			url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
			return url;

		if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) &&
			(absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
			return absolute.AbsoluteUri;

		return Uri.TryCreate(sourceUrl, UriKind.Absolute, out var source) &&
			Uri.TryCreate(source, url, out var resolved)
			? resolved.AbsoluteUri
			: url;
	}

    public static string FormatFileName(FullBookScaffold book)
    {
        static string Clean(string input, int maxLength = 32)
        {
            const char REPLACER = '-';
            string REPLACER_STR = $"{REPLACER}{REPLACER}";
            var alphaNumeric = new Regex("[^a-zA-Z0-9-]");
            var cleaned = alphaNumeric.Replace(input, REPLACER.ToString())
                .Trim()
                .TrimEnd('-')
                .ToLower();
            while (cleaned.Contains(REPLACER_STR))
                cleaned = cleaned.Replace(REPLACER_STR, REPLACER.ToString());

            if (cleaned.Length <= maxLength) return cleaned;

            var firstIndex = cleaned.IndexOf(REPLACER);
            if (firstIndex == -1 || firstIndex >= maxLength)
                return cleaned[..32];

            var output = "";
            var parts = cleaned.Split(REPLACER, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (output.Length + part.Length + 1 > maxLength)
                    break;

                output += $"{part}{REPLACER}";
            }

            return output.TrimEnd(REPLACER);
        }

        return $"{Clean(book.Series.Title)}-volume-{book.Book.Ordinal}.epub".PurgePathChars();
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

	public string GetImageExtension(string type)
	{
		return type switch
		{
			"image/jpeg" => "jpg",
			"image/jpg" => "jpg",
			"image/png" => "png",
			"image/webp" => "webp",
			"image/gif" => "gif",
			"image/svg+xml" => "svg",
			_ => throw new NotSupportedException($"`{type}` is not a known media-type")
		};
	}

	public string DetermineName(string url, string name, string type)
	{
		name = name.Replace("\"", "").PurgePathChars();
		var mediaType = type.Split(';')[0].Trim().ToLowerInvariant();
		if (mediaType.StartsWith("image/", StringComparison.Ordinal))
		{
			// Image proxies can return JPEG bytes for a .webp URL. The EPUB
			// manifest uses this extension, so it must agree with the response type.
			if (string.IsNullOrEmpty(name))
			{
				var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url.Split('?', '#')[0];
				name = Path.GetFileName(path).PurgePathChars();
			}
			name = Path.ChangeExtension(string.IsNullOrEmpty(name) ? "image" : name, GetImageExtension(mediaType));
		}
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

	public async Task<StreamResult> DownloadFile(string url)
	{
		var semaphore = _fileDownloads.GetOrAdd(FileCacheService.NormalizeUrl(url), _ => new SemaphoreSlim(1, 1));

		try
        {
            await semaphore.WaitAsync();
            return await _file.GetFile(url);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while downloading file: {url}", url);
			throw;
        }
		finally
		{
			semaphore.Release();
		}
    }

    
	public async Task<StreamResult> GetData(string url, bool skipTransform = false)
	{
		//if (url.ToLower().StartsWith("https://static.index-0.com/"))
		//	url = url.Replace("https://static.index-0.com/", "file://C:/users/cardboard/documents/local-files/");

		var output = TryGetDataUrl(url, out var embedded)
			? embedded
			: await (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ? GetDataFromFile(url.Remove(0, 7)) : DownloadFile(url));

		if (output.Mimetype != "image/webp" || skipTransform) return output;

		await using var source = output.Stream;
		var pngStream = new MemoryStream();
		try
		{
			await source.ConvertImage(pngStream, ImageMagick.MagickFormat.Png);
		}
		catch
		{
			await pngStream.DisposeAsync();
			throw;
		}
		pngStream.Position = 0;

		return new StreamResult(pngStream, $"{Path.GetFileNameWithoutExtension(output.Name)}.png", "image/png");
	}

	public static bool TryGetDataUrl(string url, out StreamResult result)
	{
		result = default!;
		if (!url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return false;

		var separator = url.IndexOf(',');
		if (separator < 5) throw new InvalidDataException("Invalid data URL.");

		var metadata = url[5..separator].Split(';', StringSplitOptions.RemoveEmptyEntries);
		var mimetype = metadata.FirstOrDefault(t => t.Contains('/')) ?? "application/octet-stream";
		var payload = url[(separator + 1)..];
		var data = metadata.Contains("base64", StringComparer.OrdinalIgnoreCase)
			? Convert.FromBase64String(payload)
			: Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));
		var extension = mimetype switch
		{
			"image/jpeg" => ".jpg",
			"image/png" => ".png",
			"image/webp" => ".webp",
			"image/gif" => ".gif",
			"image/svg+xml" => ".svg",
			_ => string.Empty,
		};
		result = new StreamResult(new MemoryStream(data), $"embedded{extension}", mimetype);
		return true;
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
				//.Replace("<hr>", "")
				//.Replace("<hr/>", "")
				//.Replace("<hr />", "")
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
