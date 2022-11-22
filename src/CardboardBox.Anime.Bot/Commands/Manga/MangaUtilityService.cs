using System.Net;
using IS = SixLabors.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Color = Discord.Color;

namespace CardboardBox.Anime.Bot.Commands
{
	public interface IMangaUtilityService
	{
		EmbedBuilder GenerateEmbed(DbManga manga);
		EmbedBuilder GenerateEmbed(MangaProgress manga);
		string GenerateRead(MangaWithChapters mangaWChap, DbMangaChapter chapter, string[] pages, int page, ulong user);
		Task<Stream> GetImage(string imageUrl);
	}

	public class MangaUtilityService : IMangaUtilityService
	{
		private const long MAX_SIZE = 8000000;
		private readonly IApiService _api;

		public MangaUtilityService(IApiService api)
		{
			_api = api;
		}

		public EmbedBuilder GenerateEmbed(DbManga manga)
		{
			var e = new EmbedBuilder()
				.WithTitle(manga.Title)
				.WithDescription(manga.Description)
				.WithColor(Color.Blue)
				.WithImageUrl(manga.Cover)
				.WithUrl("https://cba.index-0.com/manga/" + manga.Id)
				.WithCurrentTimestamp()
				.WithFooter("CardboardBox | Manga")
				.AddOptField("Source", $"[{manga.Provider}]({manga.Url})")
				.AddOptField("Tags", string.Join(", ", manga.Tags));

			for (var i = 0; i < manga.AltTitles.Length && i < 20; i++)
				e.AddOptField("Alt Title", manga.AltTitles[i]);

			return e;
		}

		public EmbedBuilder GenerateEmbed(MangaProgress manga) => GenerateEmbed(manga.Manga);

		public string GenerateRead(MangaWithChapters mangaWChap, DbMangaChapter chapter, string[] pages, int page, ulong user)
		{
			var manga = mangaWChap.Manga;
			var url = $"https://cba.index-0.com/manga/{manga.Id}/{chapter.Id}/{page + 1}";

			var ci = mangaWChap.Chapters.IndexOf(t => t.Id == chapter.Id);
			return $"{manga.Title}\r\n[Read Ch. #{chapter.Ordinal} P. #{page + 1} Online](<{url}>)\r\n" +
				$"Chapters: {ci + 1}/{mangaWChap.Chapters.Length}. Pages: {page + 1}/{pages.Length}\r\n" +
				$"<@{user}> is reading this!";
		}

		public async Task<Stream> GetImage(string imageUrl)
		{
			var uri = WebUtility.UrlEncode(imageUrl);
			var url = $"https://cba-proxy.index-0.com/proxy?path={uri}&group=manga-page";
			var (stream, length, _, _) = await _api.GetData(url);
			if (length >= MAX_SIZE)
				stream = await ResizeImage(stream);
			return stream;
		}

		public async Task<Stream> ResizeImage(Stream data)
		{
			var output = new MemoryStream();
			using var io = data;
			using var image = await IS.Image.LoadAsync(io);

			int w = (int)(image.Width / 0.66), h = (int)(image.Height / 0.66);
			image.Mutate(x => x.Resize(w, h));
			await image.SaveAsJpegAsync(output);
			output.Position = 0;
			return output;
		}
	}
}
