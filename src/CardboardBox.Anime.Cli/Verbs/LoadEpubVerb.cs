using CardboardBox.Extensions;
using CommandLine;
using System.Globalization;
using System.IO.Compression;

namespace CardboardBox.Anime.Cli.Verbs;

using LightNovel.Core;
using LightNovel.Core.Sources.Utilities;

[Verb("load-epub", HelpText = "Loads one or more EPUB light novels into the database")]
public class LoadEpubOptions
{
	[Value(0, MetaName = "epubs", HelpText = "EPUB files or directories containing EPUB files", Required = true)]
	public IEnumerable<string> Entries { get; set; } = [];

	[Option('s', "series-id", HelpText = "Add every EPUB to an existing series instead of deriving the series from EPUB metadata")]
	public long? SeriesId { get; set; }

	[Option('t', "series-title", HelpText = "Override the series title when creating a new series")]
	public string? SeriesTitle { get; set; }
}

internal class LoadEpubVerb(
	ILnDbService _db,
	ISmartReaderService _smart,
	ILogger<LoadEpubVerb> logger) : BooleanVerb<LoadEpubOptions>(logger)
{
	public override async Task<bool> Execute(LoadEpubOptions options, CancellationToken token)
	{
		var files = ResolveFiles(options.Entries).ToArray();
		if (files.Length == 0)
		{
			_logger.LogError("No EPUB files were found in: {Entries}", string.Join(", ", options.Entries));
			return false;
		}

		Series? selectedSeries = null;
		if (options.SeriesId is not null)
		{
			selectedSeries = await _db.Series.Fetch(options.SeriesId.Value);
			if (selectedSeries is null)
			{
				_logger.LogError("Series with ID {SeriesId} was not found.", options.SeriesId);
				return false;
			}
		}

		var succeeded = true;
		var epubs = new List<(string File, EpubFile Epub)>();
		foreach (var file in files)
		{
			token.ThrowIfCancellationRequested();
			try
			{
				epubs.Add((file, ReadEpub(file)));
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				succeeded = false;
				_logger.LogError(ex, "Failed to load EPUB {File}.", file);
			}
		}

		var knownSeries = selectedSeries is null
			? BuildSeriesLookup(await _db.Series.All())
			: new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
		foreach (var (file, epub) in epubs
			.OrderBy(t => options.SeriesTitle ?? t.Epub.SeriesTitle ?? t.Epub.BookTitle, StringComparer.OrdinalIgnoreCase)
			.ThenBy(t => t.Epub.SeriesPosition ?? long.MaxValue)
			.ThenBy(t => t.Epub.BookTitle, StringComparer.OrdinalIgnoreCase))
		{
			token.ThrowIfCancellationRequested();
			try
			{
				var series = selectedSeries ?? await GetOrCreateSeries(epub, options.SeriesTitle, knownSeries);
				await Import(series, epub, file, token);
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				succeeded = false;
				_logger.LogError(ex, "Failed to import EPUB {File}.", file);
			}
		}

		return succeeded && epubs.Count > 0;
	}

	private Dictionary<string, Series> BuildSeriesLookup(IEnumerable<Series> series)
	{
		var output = new Dictionary<string, Series>(StringComparer.OrdinalIgnoreCase);
		foreach (var matches in series.GroupBy(t => t.HashId, StringComparer.OrdinalIgnoreCase))
		{
			var canonicalHash = matches.Key.ToLowerInvariant();
			var candidates = matches
				.OrderByDescending(t => t.HashId.Equals(canonicalHash, StringComparison.Ordinal))
				.ThenBy(t => t.Id)
				.ToArray();
			var selected = candidates[0];
			output[selected.HashId] = selected;

			if (candidates.Length > 1)
			{
				_logger.LogWarning(
					"Multiple active series have the same hash {HashId}. Using series {SeriesId}; conflicting IDs: {ConflictingIds}.",
					matches.Key,
					selected.Id,
					string.Join(", ", candidates.Skip(1).Select(t => t.Id)));
			}
		}
		return output;
	}

	public static IEnumerable<string> ResolveFiles(IEnumerable<string> entries)
	{
		return entries
			.SelectMany(entry => Directory.Exists(entry)
				? Directory.EnumerateFiles(entry, "*.epub", SearchOption.TopDirectoryOnly)
				: File.Exists(entry) && Path.GetExtension(entry).Equals(".epub", StringComparison.OrdinalIgnoreCase)
					? [entry]
					: Array.Empty<string>())
			.Select(Path.GetFullPath)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase);
	}

	private async Task<Series> GetOrCreateSeries(
		EpubFile epub,
		string? titleOverride,
		Dictionary<string, Series> knownSeries)
	{
		var title = CleanText(titleOverride) ?? epub.SeriesTitle ?? epub.BookTitle;
		var hash = title.MD5Hash();
		if (knownSeries.TryGetValue(hash, out var existing))
		{
			_logger.LogInformation("Using existing series {SeriesId} - {SeriesTitle}.", existing.Id, existing.Title);
			return existing;
		}

		var series = new Series
		{
			HashId = hash,
			Title = title,
			Url = $"epub://{hash}",
			LastChapterUrl = string.Empty,
			Description = epub.Description,
			Image = epub.Cover,
			Genre = epub.Subjects,
			Tags = [],
			Authors = epub.Authors,
			Illustrators = epub.Illustrators,
			Editors = epub.Editors,
			Translators = epub.Translators,
		};
		series.Id = await _db.Series.Upsert(series);
		knownSeries[hash] = series;
		_logger.LogInformation("Created series {SeriesId} - {SeriesTitle} from EPUB metadata.", series.Id, series.Title);
		return series;
	}

	private async Task Import(Series series, EpubFile epub, string file, CancellationToken token)
	{
		var scaffold = await _db.Series.Scaffold(series.Id);
		var books = scaffold?.Books.Select(t => t.Book).ToArray() ?? [];
		var bookHash = epub.BookTitle.MD5Hash();
		var book = books.FirstOrDefault(t => t.HashId.Equals(bookHash, StringComparison.OrdinalIgnoreCase));
		if (book is null)
		{
			var usedOrdinals = books.Select(t => t.Ordinal).ToHashSet();
			var ordinal = epub.SeriesPosition is > 0 && !usedOrdinals.Contains(epub.SeriesPosition.Value)
				? epub.SeriesPosition.Value
				: (books.Select(t => t.Ordinal).DefaultIfEmpty(0).Max() + 1);

			book = new Book
			{
				HashId = bookHash,
				Title = epub.BookTitle,
				Ordinal = ordinal,
				SeriesId = series.Id,
				CoverImage = epub.Cover ?? series.Image,
				Authors = epub.Authors,
				Illustrators = epub.Illustrators,
				Editors = epub.Editors,
				Translators = epub.Translators,
			};
			book.Id = await _db.Books.Upsert(book);
			_logger.LogInformation("Created book {BookId} - {BookTitle}.", book.Id, book.Title);
		}

		var flattened = scaffold is null ? [] : ManualPatreonLoadVerb.Unnest(scaffold).ToArray();
		var pageHashes = flattened.Select(t => t.Page.HashId).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var pageOrdinal = flattened.Select(t => t.Page.Ordinal).DefaultIfEmpty(0).Max();
		var chapterOrdinal = flattened
			.Where(t => t.Book.Id == book.Id)
			.Select(t => t.Chapter.Ordinal)
			.DefaultIfEmpty(0)
			.Max();
		var lastPage = flattened.OrderByDescending(t => t.Page.Ordinal).FirstOrDefault()?.Page;
		var loaded = 0;

		foreach (var sourceChapter in epub.Chapters)
		{
			token.ThrowIfCancellationRequested();
			var url = $"epub://{series.HashId}/{book.HashId}/{Uri.EscapeDataString(sourceChapter.Path)}";
			var pageHash = url.MD5Hash();
			if (pageHashes.Contains(pageHash))
			{
				_logger.LogDebug("Skipping chapter already in the database: {Title}.", sourceChapter.Title);
				continue;
			}

			if (lastPage is not null && !string.Equals(lastPage.NextUrl, url, StringComparison.Ordinal))
			{
				lastPage.NextUrl = url;
				await _db.Pages.Update(lastPage);
			}

			chapterOrdinal++;
			pageOrdinal++;
			var chapter = new Chapter
			{
				HashId = sourceChapter.Path.MD5Hash(),
				Title = sourceChapter.Title,
				Ordinal = chapterOrdinal,
				BookId = book.Id,
			};
			chapter.Id = await _db.Chapters.Upsert(chapter);

			var page = new Page
			{
				HashId = pageHash,
				Title = sourceChapter.Title,
				Ordinal = pageOrdinal,
				SeriesId = series.Id,
				Url = url,
				Content = CleanChapter(sourceChapter, epub),
				Mimetype = "application/html",
			};
			page.Id = await _db.Pages.Upsert(page);

			await _db.ChapterPages.Upsert(new ChapterPage
			{
				ChapterId = chapter.Id,
				PageId = page.Id,
				Ordinal = 0,
			});

			lastPage = page;
			pageHashes.Add(pageHash);
			loaded++;
			_logger.LogInformation("Loaded chapter {ChapterOrdinal}: {ChapterTitle}.", chapterOrdinal, chapter.Title);
		}

		if (lastPage is not null && !string.Equals(series.LastChapterUrl, lastPage.Url, StringComparison.Ordinal))
		{
			series.LastChapterUrl = lastPage.Url;
			await _db.Series.Update(series);
		}

		_logger.LogInformation(
			"Loaded {Loaded} new chapters from {File} into [{SeriesId}::{SeriesTitle}] / [{BookId}::{BookTitle}].",
			loaded, file, series.Id, series.Title, book.Id, book.Title);
	}

	private string CleanChapter(EpubChapter chapter, EpubFile epub)
	{
		var doc = new HtmlDocument();
		doc.LoadHtml(chapter.Content);
		var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
		var images = doc.DocumentNode.SelectNodes("//img[@src]") ?? Enumerable.Empty<HtmlNode>();
		var imageNumber = 0;
		foreach (var image in images)
		{
			var src = image.GetAttributeValue("src", string.Empty);
			if (string.IsNullOrWhiteSpace(src) || src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
				continue;

			var path = ResolvePath(GetDirectory(chapter.Path), src);
			if (!epub.Resources.TryGetValue(path, out var resource))
				continue;

			var placeholder = $"https://epub.invalid/assets/{imageNumber++}";
			image.SetAttributeValue("src", placeholder);
			replacements[placeholder] = ToDataUrl(resource.MediaType, resource.Data);
		}

		var body = doc.DocumentNode.SelectSingleNode("//*[local-name()='body']");
		var clean = body?.InnerHtml ?? doc.DocumentNode.InnerHtml;
		foreach (var replacement in replacements)
			clean = clean.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
		return clean;
	}

	public static EpubFile ReadEpub(string path)
	{
		using var archive = ZipFile.OpenRead(path);
		var entries = archive.Entries
			.Where(t => !string.IsNullOrEmpty(t.Name))
			.ToDictionary(t => NormalizePath(t.FullName), StringComparer.OrdinalIgnoreCase);

		var container = LoadXml(GetEntry(entries, "META-INF/container.xml"));
		var opfPath = container.Descendants()
			.FirstOrDefault(t => t.Name.LocalName == "rootfile")
			?.Attributes()
			.FirstOrDefault(t => t.Name.LocalName == "full-path")
			?.Value;
		if (string.IsNullOrWhiteSpace(opfPath))
			throw new InvalidDataException("The EPUB container does not specify an OPF package document.");

		opfPath = NormalizePath(opfPath);
		var opf = LoadXml(GetEntry(entries, opfPath));
		var opfDirectory = GetDirectory(opfPath);
		var metadata = opf.Descendants().FirstOrDefault(t => t.Name.LocalName == "metadata")
			?? throw new InvalidDataException("The EPUB package does not contain metadata.");

		var manifest = opf.Descendants()
			.Where(t => t.Name.LocalName == "item")
			.Select(t => new ManifestItem(
				Attribute(t, "id") ?? string.Empty,
				ResolvePath(opfDirectory, Attribute(t, "href") ?? string.Empty),
				Attribute(t, "media-type") ?? string.Empty,
				Attribute(t, "properties") ?? string.Empty))
			.Where(t => !string.IsNullOrWhiteSpace(t.Id) && !string.IsNullOrWhiteSpace(t.Path))
			.ToDictionary(t => t.Id, StringComparer.Ordinal);

		var resources = manifest.Values
			.Where(t => t.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && entries.ContainsKey(t.Path))
			.ToDictionary(
				t => t.Path,
				t => new EpubResource(t.MediaType, ReadBytes(entries[t.Path])),
				StringComparer.OrdinalIgnoreCase);

		var labels = ReadNavigationLabels(manifest.Values, entries);
		var coverPaths = opf.Descendants()
			.Where(t => t.Name.LocalName == "reference" && Attribute(t, "type")?.Contains("cover", StringComparison.OrdinalIgnoreCase) == true)
			.Select(t => ResolvePath(opfDirectory, Attribute(t, "href") ?? string.Empty))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var chapters = new List<EpubChapter>();
		foreach (var itemRef in opf.Descendants().Where(t => t.Name.LocalName == "itemref"))
		{
			if (Attribute(itemRef, "linear")?.Equals("no", StringComparison.OrdinalIgnoreCase) == true)
				continue;

			var id = Attribute(itemRef, "idref");
			if (id is null || !manifest.TryGetValue(id, out var item) || !IsHtml(item) || IsNavigation(item))
				continue;
			if (!entries.TryGetValue(item.Path, out var entry))
				continue;

			var html = ReadText(entry);
			var doc = new HtmlDocument();
			doc.LoadHtml(html);
			var body = doc.DocumentNode.SelectSingleNode("//*[local-name()='body']");
			var text = CleanText(body?.InnerText);
			var hasImage = body?.SelectSingleNode(".//img") is not null;
			var looksLikeCover = coverPaths.Contains(item.Path) ||
				((item.Id.Contains("cover", StringComparison.OrdinalIgnoreCase) || item.Path.Contains("cover", StringComparison.OrdinalIgnoreCase)) &&
				 string.IsNullOrWhiteSpace(text) && hasImage);
			if (looksLikeCover || (string.IsNullOrWhiteSpace(text) && !hasImage))
				continue;

			var title = labels.GetValueOrDefault(item.Path)
				?? CleanText(doc.DocumentNode.SelectSingleNode("//*[local-name()='h1' or local-name()='h2' or local-name()='h3']")?.InnerText)
				?? CleanText(doc.DocumentNode.SelectSingleNode("//*[local-name()='title']")?.InnerText)
				?? Path.GetFileNameWithoutExtension(item.Path);
			chapters.Add(new EpubChapter(title, item.Path, html));
		}

		if (chapters.Count == 0)
			throw new InvalidDataException("The EPUB spine does not contain any readable HTML chapters.");

		var bookTitle = ElementValue(metadata, "title") ?? Path.GetFileNameWithoutExtension(path);
		var (seriesTitle, seriesPosition) = ReadCollection(metadata);
		var creators = ReadCreators(metadata);
		var cover = ReadCover(metadata, manifest, resources);

		return new EpubFile(
			bookTitle,
			seriesTitle,
			seriesPosition,
			ElementValue(metadata, "description"),
			metadata.Elements().Where(t => t.Name.LocalName == "subject").Select(t => CleanText(t.Value)).OfType<string>().Distinct().ToArray(),
			creators.GetValueOrDefault("aut") ?? [],
			creators.GetValueOrDefault("ill") ?? [],
			creators.GetValueOrDefault("edt") ?? [],
			creators.GetValueOrDefault("trl") ?? [],
			cover,
			chapters.ToArray(),
			resources);
	}

	private static Dictionary<string, string> ReadNavigationLabels(
		IEnumerable<ManifestItem> manifest,
		Dictionary<string, ZipArchiveEntry> entries)
	{
		var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in manifest.Where(t => IsNavigation(t)))
		{
			if (!entries.TryGetValue(item.Path, out var entry))
				continue;

			var directory = GetDirectory(item.Path);
			if (item.MediaType.Equals("application/x-dtbncx+xml", StringComparison.OrdinalIgnoreCase))
			{
				var ncx = LoadXml(entry);
				foreach (var navPoint in ncx.Descendants().Where(t => t.Name.LocalName == "navPoint"))
				{
					var href = navPoint.Descendants().FirstOrDefault(t => t.Name.LocalName == "content")?.Attribute("src")?.Value;
					var label = CleanText(navPoint.Descendants().FirstOrDefault(t => t.Name.LocalName == "navLabel")?.Value);
					if (!string.IsNullOrWhiteSpace(href) && label is not null)
						output.TryAdd(ResolvePath(directory, href), label);
				}
				continue;
			}

			var doc = new HtmlDocument();
			doc.LoadHtml(ReadText(entry));
			foreach (var anchor in doc.DocumentNode.SelectNodes("//a[@href]") ?? Enumerable.Empty<HtmlNode>())
			{
				var href = anchor.GetAttributeValue("href", string.Empty);
				var label = CleanText(anchor.InnerText);
				if (!string.IsNullOrWhiteSpace(href) && !href.StartsWith('#') && label is not null)
					output.TryAdd(ResolvePath(directory, href), label);
			}
		}
		return output;
	}

	private static (string? title, long? position) ReadCollection(XElement metadata)
	{
		var meta = metadata.Elements().Where(t => t.Name.LocalName == "meta").ToArray();
		var collections = meta.Where(t => Attribute(t, "property") == "belongs-to-collection").ToArray();
		var collection = collections.FirstOrDefault(t =>
		{
			var id = Attribute(t, "id");
			return id is not null && meta.Any(r => Attribute(r, "refines") == $"#{id}" &&
				Attribute(r, "property") == "collection-type" && r.Value.Equals("series", StringComparison.OrdinalIgnoreCase));
		}) ?? collections.FirstOrDefault();

		if (collection is not null)
		{
			var id = Attribute(collection, "id");
			var rawPosition = id is null ? null : meta.FirstOrDefault(t =>
				Attribute(t, "refines") == $"#{id}" && Attribute(t, "property") == "group-position")?.Value;
			return (CleanText(collection.Value), ParsePosition(rawPosition));
		}

		var calibreTitle = meta.FirstOrDefault(t => Attribute(t, "name") == "calibre:series");
		var calibrePosition = meta.FirstOrDefault(t => Attribute(t, "name") == "calibre:series_index");
		return (CleanText(Attribute(calibreTitle, "content")), ParsePosition(Attribute(calibrePosition, "content")));
	}

	private static Dictionary<string, string[]> ReadCreators(XElement metadata)
	{
		var meta = metadata.Elements().Where(t => t.Name.LocalName == "meta").ToArray();
		return metadata.Elements()
			.Where(t => t.Name.LocalName == "creator")
			.Select(t =>
			{
				var id = Attribute(t, "id");
				var role = Attribute(t, "role") ?? (id is null ? null : meta.FirstOrDefault(r =>
					Attribute(r, "refines") == $"#{id}" && Attribute(r, "property") == "role")?.Value) ?? "aut";
				return (role: role.Trim().ToLowerInvariant(), name: CleanText(t.Value));
			})
			.Where(t => t.name is not null)
			.GroupBy(t => t.role)
			.ToDictionary(t => t.Key, t => t.Select(a => a.name!).Distinct().ToArray());
	}

	private static string? ReadCover(
		XElement metadata,
		Dictionary<string, ManifestItem> manifest,
		Dictionary<string, EpubResource> resources)
	{
		var cover = manifest.Values.FirstOrDefault(t => t.Properties.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Contains("cover-image", StringComparer.OrdinalIgnoreCase));
		if (cover is null)
		{
			var coverId = metadata.Elements().FirstOrDefault(t => t.Name.LocalName == "meta" && Attribute(t, "name") == "cover");
			var id = Attribute(coverId, "content");
			if (id is not null)
				manifest.TryGetValue(id, out cover);
		}

		return cover is not null && resources.TryGetValue(cover.Path, out var resource)
			? ToDataUrl(resource.MediaType, resource.Data)
			: null;
	}

	private static string? Attribute(XElement? element, string name) => element?.Attributes()
		.FirstOrDefault(t => t.Name.LocalName == name)?.Value;

	private static string? ElementValue(XElement parent, string name) => CleanText(parent.Elements()
		.FirstOrDefault(t => t.Name.LocalName == name)?.Value);

	private static string? CleanText(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)) return null;
		return Regex.Replace(HtmlEntity.DeEntitize(value), "\\s+", " ").Trim();
	}

	private static long? ParsePosition(string? value)
	{
		return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
			? (long)Math.Ceiling(parsed)
			: null;
	}

	private static bool IsHtml(ManifestItem item) =>
		item.MediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase) ||
		item.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
		item.Path.EndsWith(".xhtml", StringComparison.OrdinalIgnoreCase) ||
		item.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

	private static bool IsNavigation(ManifestItem item) =>
		item.MediaType.Equals("application/x-dtbncx+xml", StringComparison.OrdinalIgnoreCase) ||
		item.Properties.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("nav", StringComparer.OrdinalIgnoreCase);

	private static XDocument LoadXml(ZipArchiveEntry entry)
	{
		using var stream = entry.Open();
		return XDocument.Load(stream);
	}

	private static string ReadText(ZipArchiveEntry entry)
	{
		using var stream = entry.Open();
		using var reader = new StreamReader(stream, Encoding.UTF8, true);
		return reader.ReadToEnd();
	}

	private static byte[] ReadBytes(ZipArchiveEntry entry)
	{
		using var input = entry.Open();
		using var output = new MemoryStream();
		input.CopyTo(output);
		return output.ToArray();
	}

	private static ZipArchiveEntry GetEntry(Dictionary<string, ZipArchiveEntry> entries, string path)
	{
		return entries.TryGetValue(NormalizePath(path), out var entry)
			? entry
			: throw new InvalidDataException($"The EPUB is missing required entry '{path}'.");
	}

	private static string ResolvePath(string directory, string href)
	{
		if (string.IsNullOrWhiteSpace(href)) return string.Empty;
		var basePath = string.IsNullOrWhiteSpace(directory) ? string.Empty : $"{directory.Trim('/')}/";
		var baseUri = new Uri($"https://epub.invalid/{basePath}");
		var resolved = new Uri(baseUri, WebUtility.HtmlDecode(href));
		return NormalizePath(Uri.UnescapeDataString(resolved.AbsolutePath));
	}

	private static string NormalizePath(string path) => path.Replace('\\', '/').TrimStart('/');

	private static string GetDirectory(string path)
	{
		var index = path.LastIndexOf('/');
		return index < 0 ? string.Empty : path[..index];
	}

	private static string ToDataUrl(string mediaType, byte[] data)
	{
		if (string.IsNullOrWhiteSpace(mediaType)) mediaType = "application/octet-stream";
		return $"data:{mediaType};base64,{Convert.ToBase64String(data)}";
	}

	private sealed record ManifestItem(string Id, string Path, string MediaType, string Properties);

	public sealed record EpubResource(string MediaType, byte[] Data);

	public sealed record EpubChapter(string Title, string Path, string Content);

	public sealed record EpubFile(
		string BookTitle,
		string? SeriesTitle,
		long? SeriesPosition,
		string? Description,
		string[] Subjects,
		string[] Authors,
		string[] Illustrators,
		string[] Editors,
		string[] Translators,
		string? Cover,
		EpubChapter[] Chapters,
		Dictionary<string, EpubResource> Resources);
}
