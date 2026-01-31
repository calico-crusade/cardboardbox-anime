using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CardboardBox.Manga;

using Anime.Core;
using Anime.Database;
using CardboardBox.Extensions;

public interface IMangaImageService
{
	Task<Stream?> Combine(MangaStripRequest req);
}

public class MangaImageService : IMangaImageService
{
	private readonly IMangaService _manga;
	private readonly IMangaDbService _db;
	private readonly IApiService _api;

	public MangaImageService(
		IMangaService manga, 
		IMangaDbService db,
		IApiService api)
	{
		_manga = manga;
		_db = db;
		_api = api;
	}

	public async Task<(Image image, Stream stream)> Load(string url)
	{
		var (io, _, _, _) = await _api.GetData(url);
		var image = await Image.LoadAsync(io);
		return (image, io);
	}

	public async IAsyncEnumerable<(Image image, Stream stream)> Load(MangaStripRequest strip)
	{
		var manga = await _db.GetManga(strip.MangaId, null);

		if (manga == null) yield break;

		foreach(var img in strip.Pages)
		{
			var chapter = manga.Chapters
				.FirstOrDefault(t => t.Id == img.ChapterId);
			if (chapter == null) continue;

			if (chapter.Pages == null || chapter.Pages.Length == 0)
				chapter.Pages = await _manga.MangaPages(chapter, false);

			if (chapter.Pages == null || chapter.Pages.Length == 0 ||
				img.Page > chapter.Pages.Length || img.Page <= 0)
				yield break;

			var image = chapter.Pages[img.Page - 1];
			yield return await Load(image);
		}
	}

	public async Task<Stream?> Combine(MangaStripRequest req)
	{
		var images = await Load(req).ToArrayAsync();

		if (images == null || images.Length <= 0) return null;

		int width = images.Max(t => t.image.Width), 
			height = images.Sum(t => t.image.Height);

		using var image = new Image<Rgba32>(width, height);

		int y = 0;
		foreach(var (img, io) in images)
		{
			int x = (width / 2) - (img.Width / 2);
			var p = new Point(x, y);
			image.Mutate(t => t.DrawImage(img, p, 1));
			y += img.Height;
		}

		var output = new MemoryStream();
		await image.SaveAsPngAsync(output);
		output.Position = 0;

		await images.Select(async t =>
		{
			t.image.Dispose();
			await t.stream.DisposeAsync();
		}).WhenAll();
		return output;
	}
}
