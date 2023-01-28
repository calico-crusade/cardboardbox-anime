using System.Net;
using IS = SixLabors.ImageSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Color = Discord.Color;

namespace CardboardBox.Anime.Bot;

using Manga;

public interface IMangaUtilityService
{
	EmbedBuilder GenerateEmbed(DbManga manga);
	EmbedBuilder GenerateEmbed(MangaProgress manga);
	EmbedBuilder GenerateEmbed(Manga manga, bool local = true);
	EmbedBuilder GenerateShortEmbed(MangaProgress manga);
	string GenerateRead(MangaWithChapters mangaWChap, DbMangaChapter chapter, string[] pages, int page, ulong user);
	Task<Stream> GetImage(string imageUrl);
	int Compute(string s, string t);
	double DiceMatch(string string1, string string2);
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

	public EmbedBuilder GenerateShortEmbed(MangaProgress progress)
	{
		var manga = progress.Manga;
		var mangaCreated = (manga.CreatedAt ?? DateTime.Now).AddMinutes(1);
		var latestChapter = progress.Stats.LatestChapter ?? DateTime.Now;
		var isNew = latestChapter < mangaCreated;

		var e = new EmbedBuilder()
			.WithTitle(manga.Title)
			.WithDescription(manga.Description)
			.WithColor(Color.Blue)
			.WithThumbnailUrl(manga.Cover)
			.WithUrl("https://cba.index-0.com/manga/" + manga.Id)
			.WithCurrentTimestamp()
			.WithFooter("CardboardBox | Manga")
			.AddOptField("Tags", string.Join(", ", manga.Tags))
			.AddOptField("Source", $"[{manga.Provider}]({manga.Url})", true)
			.AddOptField("Update Type", isNew ? "New Upload" : "New Chapter", true);

		if (manga.Nsfw)
			e.AddOptField("NSFW", "yes", true);

		return e;
	}

	public EmbedBuilder GenerateEmbed(Manga manga, bool local = true)
	{
		var e = new EmbedBuilder()
			.WithTitle(manga.Title)
			.WithDescription(manga.Description)
			.WithColor(Color.Blue)
			.WithThumbnailUrl(manga.Cover)
			.WithUrl(local ? "https://cba.index-0.com/manga/" + manga.Id : "https://mangadex.org/title/" + manga.Id)
			.WithCurrentTimestamp()
			.WithFooter("CardboardBox | Manga")
			.AddOptField("Tags", string.Join(", ", manga.Tags))
			.AddOptField("Source", $"[{manga.Provider}]({manga.HomePage})", true);

		if (manga.Nsfw)
			e.AddOptField("NSFW", "yes", true);

		return e;
	}

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

	public int Compute(string s, string t)
	{
		if (string.IsNullOrEmpty(s))
		{
			if (string.IsNullOrEmpty(t))
				return 0;
			return t.Length;
		}

		if (string.IsNullOrEmpty(t))
		{
			return s.Length;
		}

		int n = s.Length;
		int m = t.Length;
		int[,] d = new int[n + 1, m + 1];

		// initialize the top and right of the table to 0, 1, 2, ...
		for (int i = 0; i <= n; d[i, 0] = i++) ;
		for (int j = 1; j <= m; d[0, j] = j++) ;

		for (int i = 1; i <= n; i++)
		{
			for (int j = 1; j <= m; j++)
			{
				int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
				int min1 = d[i - 1, j] + 1;
				int min2 = d[i, j - 1] + 1;
				int min3 = d[i - 1, j - 1] + cost;
				d[i, j] = Math.Min(Math.Min(min1, min2), min3);
			}
		}
		return d[n, m];
	}

	public double DiceMatch(string string1, string string2)
	{
		if (string.IsNullOrEmpty(string1) || string.IsNullOrEmpty(string2))
			return 0;

		if (string1 == string2)
			return 1;

		int strlen1 = string1.Length;
		int strlen2 = string2.Length;

		if (strlen1 < 2 || strlen2 < 2)
			return 0;

		int length1 = strlen1 - 1;
		int length2 = strlen2 - 1;

		double matches = 0;
		int i = 0;
		int j = 0;

		while (i < length1 && j < length2)
		{
			string a = string1.Substring(i, 2);
			string b = string2.Substring(j, 2);
			int cmp = string.Compare(a, b);

			if (cmp == 0)
				matches += 2;

			++i;
			++j;
		}

		return matches / (length1 + length2);
	}
}
