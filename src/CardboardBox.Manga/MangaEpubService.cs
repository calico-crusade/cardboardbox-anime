namespace CardboardBox.Manga;

using Anime.Core;
using Anime.Database;
using Epub;

public interface IMangaEpubService
{
	Task<StreamResult?> Generate(long mangaId, params long[] chapterIds);
}

public class MangaEpubService : IMangaEpubService
{
	private readonly IMangaService _manga;
	private readonly IFileCacheService _file;

	public MangaEpubService(
		IMangaService manga, 
		IFileCacheService file)
	{
		_manga = manga;
		_file = file;
	}

	public async Task<StreamResult?> Generate(long mangaId, params long[] chapterIds)
	{
		var mwc = await _manga.Manga(mangaId, null);
		if (mwc == null || mwc.Manga == null || 
			mwc.Chapters == null || mwc.Chapters.Length == 0) return null;

		var (manga, chapters) = mwc;
		var cwp = await 
			(chapterIds.Length == 0 ? chapters : chapters.Where(t => chapterIds.Contains(t.Id)))
			.Select(async t =>
			{
				if (t.Pages.Length == 0)
					t.Pages = await _manga.MangaPages(t.Id, false);

				return t;
			})
			.WhenAll();

		if (cwp.Length == 0) return null;

		var io = new MemoryStream();
        await GenerateRaw(io, manga, cwp);

		io.Position = 0;
		return new(io, $"{manga.Title}.epub".PurgePathChars(), "application/epub+zip");
	}

	public async Task GenerateRaw(Stream stream, DbManga manga, DbMangaChapter[] chapters)
	{
		await using (var epub = EpubBuilder.Create(manga.Title, stream))
		{
			var bob = await epub.Start();
			bob.BelongsTo(manga.Title, 1);

			await HandleConverImage(bob, manga);
			await bob.AddStylesheetFromFile("stylesheet.css", "stylesheet.css");

			foreach (var chap in chapters)
				await HandleChapter(bob, chap);
		}
	}

	public async Task HandleChapter(IEpubBuilder bob, DbMangaChapter chapter)
	{
		await bob.AddChapter(chapter.Title, async c =>
		{
			foreach(var page in chapter.Pages)
			{
				var (data, file, type) = await GetData(page);
				var name = DetermineName(page, file, type);
				await c.AddImage(name, data);
				await data.DisposeAsync();
			}
		});
	}

	public async Task HandleConverImage(IEpubBuilder bob, DbManga manga)
	{
		var (data, file, type) = await GetData(manga.Cover);
		var name = DetermineName(manga.Cover, file, type);
		await bob.AddCoverImage(name, data);
		await data.DisposeAsync();
	}

	public Task<StreamResult> GetData(string url)
	{
		if (url.ToLower().StartsWith("file://")) return GetDataFromFile(url.Remove(0, 7));
		return _file.GetFile(url);
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
}

