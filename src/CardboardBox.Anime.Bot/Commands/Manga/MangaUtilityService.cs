﻿using System.Net;

namespace CardboardBox.Anime.Bot.Commands
{
	public interface IMangaUtilityService
	{
		EmbedBuilder GenerateEmbed(DbManga manga);
		string GenerateRead(MangaWithChapters mangaWChap, DbMangaChapter chapter, string[] pages, int page, ulong user);
		Task<Stream> GetImage(string imageUrl);
	}

	public class MangaUtilityService : IMangaUtilityService
	{
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

		public string GenerateRead(MangaWithChapters mangaWChap, DbMangaChapter chapter, string[] pages, int page, ulong user)
		{
			var manga = mangaWChap.Manga;
			var url = $"https://cba.index-0.com/manga/{manga.Id}/{chapter.Id}/{page + 1}";

			var ci = mangaWChap.Chapters.IndexOf(t => t.Id == chapter.Id);
			return $"{manga.Title}\r\n[Read Ch. #{chapter.Ordinal} P. #{page + 1} Online]({url})\r\n" +
				$"Chapters: {ci + 1}/{mangaWChap.Chapters.Length}. Pages: {page + 1}/{pages.Length}\r\n" +
				$"<@{user}> is reading this!";
		}

		public async Task<Stream> GetImage(string imageUrl)
		{
			var uri = WebUtility.UrlEncode(imageUrl);
			var url = $"https://cba-proxy.index-0.com/proxy?path={uri}&group=manga-page";
			var (stream, _, _, _) = await _api.GetData(url);
			return stream;
		}
	}
}