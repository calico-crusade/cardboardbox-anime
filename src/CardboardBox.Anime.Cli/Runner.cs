using SLImage = SixLabors.ImageSharp.Image;
using CardboardBox.Extensions;

namespace CardboardBox.Anime.Cli;

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
using LightNovel.Core.Sources.ZirusSource;
using CChapter = LightNovel.Core.Chapter;

using Manga;
using Manga.MangaDex;
using Manga.Providers;

using AImage = Core.Models.Image;
using MangaDexSharp;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;
using CardboardBox.Json;

public interface IRunner
{
	Task<int> Run(string[] args);
}

public class Runner(
	IVrvApiService vrv,
	ILogger<Runner> logger,
	IFunimationApiService fun,
	IApiService api,
	IAnimeMongoService mongo,
	IHiDiveApiService hidive,
	IAnimeDbService db,
	ICrunchyrollApiService crunchy,
	IJsonService _json,
	IOldLnApiService ln,
	IChapterDbService chapDb,
	IPdfService pdf,
	ILnDbService lnDb,
	INovelApiService napi,
	INovelUpdatesService info,
	IReLibSourceService reL,
	IMangaDexService mangaDex,
	IMangaClashSource mangaClash,
	INhentaiSource nhentai,
	IMangaService manga,
	IMangaMatchService match,
	IMangaCacheDbService cacheDb,
	IMangaDbService mangaDb,
	IBattwoSource battwo,
	ILntSourceService lnt,
	INyxSourceService nyx,
	IPurgeUtils purge,
	IZirusMusingsSourceService zirus,
	INncSourceService nnc,
	IRawKumaSource kuma,
	IBakaPervertSourceService baka,
	IFanTransSourceService ftl,
	IHeadCanonTLSourceService headCanon,
	IMagicHouseSourceService magicHouse,
	IVampiramtlSourceService vampiramtl,
	IChapmanganatoSource manganato,
	IMarkdownService markdown,
	IFlareSolver flare,
	IRoyalRoadSourceService royalRoad,
	IStorySeedlingSourceService storySeedling,
	ICardboardTranslationsSourceService ctl,
    INovelBinSourceService _nbs,
	ILikeMangaSource _lkm,
	ILONAMMTLSourceService _lonammtl,
	IWeebDexSource _wd,
	IComixSource _comix) : IRunner
{
	private const string VRV_JSON = "vrv2.json";
	private const string FUN_JSON = "fun.json";
    private readonly ILogger _logger = logger;
    private readonly INovelUpdatesService _info = info;
    private readonly IMangaService _manga = manga;
    private readonly IMarkdownService _markdown = markdown;

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
				case "sizes": await DetermineImageSizes(); break;
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
				case "manga": await TestManga(); break;
				case "index": await Index(); break;
				case "fix-cache": await FixCache(); break;
				case "index-db": await IndexDbImages(); break;
				case "index-covers": await IndexCovers(); break;
				case "battwo": await TestBattow(); break;
				case "tags": await FixTags(); break;
				case "progress": await FixProgress(); break;
				case "lnt": await TestLnt(); break;
				case "nuchaps": await TestNUChapters(); break;
				case "lntfix": await LntImageFix(); break;
				case "nyx": await Nyx(); break;
				case "zirus": await ZirusTest(); break;
				case "fetishes": await Fetishes(); break;
				case "nncon-images": await NnconImages(); break;
                case "nncon-load": await NnconLoad(); break;
				case "fix-yururi": await FixYururiBooking(); break;
				case "kuma": await DownloadChapters(); break;
				case "baka": await Baka(); break;
				case "ftl": await Ftl(); break;
				case "head-canon": await HeadCanon(); break;
				case "magic-house": await MagicHouse(); break;
				case "vampiramtl": await Vampiramtl(); break;
				case "manganato": await Manganato(); break;
				case "fix-html": await FixBadHtml(); break;
				case "japanese": await CheckJapaneseSmartReader(); break;
				case "royalroad": await RoyalRoad(); break;
				case "story-seedling": await StorySeedling(); break;
				case "ctl": await CTLTest(); break;
				case "nbs": await NBSTest(); break;
				case "lkm": await LKMTest(); break;
				case "index-manga": await IndexManga(); break;
				case "lonammt": await LONAMMTLTest(); break;
				case "weebdex": await WeebDex(); break;
				case "comix": await Comix(); break;
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

	public async Task Comix()
	{
		const string URL = "https://comix.to/title/j3zd-defying-expectations-with-gravity-magic-to-be-unparalleled";

		var api = _comix;
		var (matches, part) = api.MatchesProvider(URL);
		if (!matches || string.IsNullOrEmpty(part))
		{
			_logger.LogError("Failed to match provider");
			return;
		}

		var manga = await api.Manga(part);
		if (manga is null)
		{
			_logger.LogError("Failed to fetch manga");
			return;
		}

		_logger.LogInformation("Manga: {title}", manga.Title);
		var pages = await api.ChapterPages(part, manga.Chapters.First().Id);
		if (pages is null)
		{
			_logger.LogError("Failed to fetch chapter");
			return;
		}

		_logger.LogInformation("Chapter: {title} - {pages} pages", pages.Title, pages.Pages.Length);
	}

	public async Task WeebDex()
	{
		const string URL = "https://weebdex.org/title/vtyi8syfjd/yuusha-no-sensei-saikyou-no-kuzu-ni-naru-s-kyuu-party-no-moto-eiyuu-ura-shakai-no-ihou-guild-de-nariagari";

		var api = _wd;
		var (matches, part) = api.MatchesProvider(URL);
		if (!matches || string.IsNullOrEmpty(part))
		{
			_logger.LogError("Failed to match provider");
			return;
		}

		var manga = await api.Manga(part);
		if (manga is null)
		{
			_logger.LogError("Failed to fetch manga");
			return;
		}

		_logger.LogInformation("Manga: {title}", manga.Title);
		var pages = await api.ChapterPages(part, manga.Chapters.First().Id);
		if (pages is null)
		{
			_logger.LogError("Failed to fetch chapter");
			return;
		}

		_logger.LogInformation("Chapter: {title} - {pages} pages", pages.Title, pages.Pages.Length);
	}

	public async Task LONAMMTLTest()
	{
		const string URL = "https://lilysobservatory.blogspot.com/2025/07/the-middle-aged-convenience-store-clerk.html";
		var series = await _lonammtl.GetSeriesInfo(URL);
		if (series is null)
		{
			_logger.LogError("Failed to fetch series info");
			return;
		}

		var volumes = _lonammtl.Volumes(URL);
		await foreach (var volume in volumes)
		{
			_logger.LogInformation("Volume: {title}", volume.Title);
			foreach (var chapter in volume.Chapters)
				_logger.LogInformation("\tChapter: {title} >> {url}", chapter.Title, chapter.Url);
		}

		if (string.IsNullOrEmpty(series.FirstChapterUrl))
		{
			_logger.LogError("No first chapter URL found");
			return;
		}

		var chap = await _lonammtl.GetChapter(series.FirstChapterUrl, "First Chapter");

		if (chap is null)
		{
			_logger.LogError("Failed to fetch chapter");
			return;
		}
	}

	public async Task LKMTest()
	{
		const string ID = "i-got-my-wish-and-reincarnated-as-the-villainess-last-boss";
		const string CID = "chapter-72";

        var manga = await _lkm.Manga(ID);
		if (manga is null)
		{
			_logger.LogWarning("Could not find manga by ID: {id}", ID);
			return;
		}

		_logger.LogInformation("Manga: {title}", manga.Title);
		var chapter = await _lkm.ChapterPages(ID, CID);
		if (chapter is null)
		{
			_logger.LogWarning("Could not find chapter by ID: {id}", CID);
			return;
        }

		_logger.LogInformation("Chapter: {title} - {pages} pages", chapter.Title, chapter.Pages.Length);
    }

	public async Task NBSTest()
	{
		const string URL = "https://novelbin.com/b/i-became-my-healer-elf-character";
		var ctx = (NovelBinSourceService)_nbs;
        var info = await ctx.ParseVolumes(new HtmlDocument(), URL).ToArrayAsync();
		if (info is null)
		{
			_logger.LogError("Failed to fetch volumes");
			return;
        }

		_logger.LogInformation("Title: {count}", info.Length);
    }

    public async Task StorySeedling()
	{
		const string URL = "https://storyseedling.com/series/138027/"; //"https://storyseedling.com/series/99893/";
        ISourceVolumeService service = storySeedling;
        async Task Info()
        {
            var info = await service.GetSeriesInfo(URL);
            if (info is null)
            {
                _logger.LogError("Failed to fetch series info");
                return;
            }

            _logger.LogInformation("Title: {Title}", info.Title);
        }

        async Task Chapter(string chapterUrl)
        {
            var chap = await service.GetChapter(chapterUrl, string.Empty);
            if (chap is null)
            {
                _logger.LogError("Failed to fetch chapter");
                return;
            }
            _logger.LogInformation("Chapter: {Title}", chap.ChapterTitle);
        }

        async Task<string[]> Volumes()
        {
            var info = await service.Volumes(URL).ToArrayAsync();
            if (info.Length == 0)
            {
                _logger.LogError("Failed to fetch volumes");
                return [];
            }

            foreach (var vol in info)
            {
                _logger.LogInformation("Volume: {Title}", vol.Title);
                foreach (var chap in vol.Chapters)
                    _logger.LogInformation("\tChapter: {Title}", chap.Title);
            }

			var all = info.SelectMany(t => t.Chapters).ToArray();
			return all.Take(2)
				.Concat(all.AReverse().Take(2))
				.Select(t => t.Url)
				.ToArray();
        }

		//await Chapter("https://storyseedling.com/series/138027/v7/0/");

        await Info();
        var chaps = await Volumes();
        if (chaps is null || chaps.Length == 0)
        {
            _logger.LogError("Failed to fetch chapter URL");
            return;
        }

        foreach (var chap in chaps)
            await Chapter(chap);
    }

	public async Task CTLTest()
	{
        //					  Is It Funny to Lose to Your Love Rival and Marry Him
        const string TITLE = "I Am a Max-level Priestess in Another World";
		var services = (CardboardTranslationsSourceService)ctl;

		var content = await services.AllEntries(TITLE).ToArrayAsync();
        if (content is null)
        {
            _logger.LogError("Failed to fetch contents");
            return;
        }

		_logger.LogInformation("Entries: {count}", content.Length);
		using var io = File.Create("ctl-content.json");
		await JsonSerializer.SerializeAsync(io, content, new JsonSerializerOptions
		{
			WriteIndented = true,
			AllowTrailingCommas = true
		});
		_logger.LogInformation("Finished");
    }

    public async Task RoyalRoad()
	{
		const string URL = "https://www.royalroad.com/fiction/44024/misadventures-incorporated";
		ISourceVolumeService service = royalRoad;
        async Task Info()
        {
            var info = await service.GetSeriesInfo(URL);
            if (info is null)
            {
                _logger.LogError("Failed to fetch series info");
                return;
            }

            _logger.LogInformation("Title: {Title}", info.Title);
        }

        async Task Chapter(string chapterUrl)
        {
            var chap = await service.GetChapter(chapterUrl, string.Empty);
            if (chap is null)
            {
                _logger.LogError("Failed to fetch chapter");
                return;
            }
            _logger.LogInformation("Chapter: {Title}", chap.ChapterTitle);
        }

        async Task<string[]> Volumes()
        {
            var info = await service.Volumes(URL).ToArrayAsync();
            if (info.Length == 0)
            {
                _logger.LogError("Failed to fetch volumes");
                return [];
            }

            foreach (var vol in info)
            {
                _logger.LogInformation("Volume: {Title}", vol.Title);
                foreach (var chap in vol.Chapters)
                    _logger.LogInformation("\tChapter: {Title}", chap.Title);
            }

			return info.SelectMany(t => t.Chapters).Take(3).Select(t => t.Url).ToArray();
        }

        await Info();
        var chaps = await Volumes();
        if (chaps is null || chaps.Length == 0)
        {
            _logger.LogError("Failed to fetch chapter URL");
            return;
        }

		foreach (var chap in chaps)
			await Chapter(chap);
    }


    public async Task CheckJapaneseSmartReader()
	{
		const string URL = "https://kakuyomu.jp/works/16817330657849409243/episodes/16817330657932337097";

		async Task<string> FetchHtml(string url)
		{
			var data = await flare.Get(url, timeout: 30_000);
            if (data is null || data.Solution is null) throw new Exception("Failed to get data");

            if (data.Solution.Status < 200 || data.Solution.Status >= 300)
                throw new Exception($"Failed to get data: {data.Solution.Status}");

			return data.Solution.Response;
        }

		async Task<string?> GetArticle(string html, string url)
		{
			var reader = new SmartReader.Reader(url, html)
			{
				Debug = true,
				LoggerDelegate = (msg) => _logger.LogDebug("[SMART READER] {url} >> {msg}", url, msg)
			};

			var article = await reader.GetArticleAsync();
            if (article is null || !article.Completed || !article.IsReadable)
            {
                var errors = article?.Errors?.ToArray() ?? [];
                foreach (var error in errors)
                    _logger.LogError(error, "[SMART READER] Failed to read >> {url}", url);
                _logger.LogWarning("Could not get article for {url}", url);
                return null;
            }

			return article.Content;
        }

		var html = await FetchHtml(URL);
        await File.WriteAllTextAsync("japanese-test.html", html);

        var article = await GetArticle(html, URL);
		await File.WriteAllTextAsync("japanese-test-article.html", article);
		_logger.LogInformation("Finished");
    }

	public async Task FixBadHtml()
    {
        const long SERIES_ID = 112;
        const string IMAGE_OUTPUT = "F:\\Pictures\\novels\\TSG";

        string Cleanse(string html)
		{
			var util = new PurgeUtils();
            var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var output = new StringBuilder();

			foreach(var node in util.Flatten(doc))
			{
				output.AppendLine(node.OuterHtml);
			}

			var clean = output.ToString();
			var markdown = _markdown.ToMarkdown(clean);
			clean = _markdown.ToHtml(markdown);

			var mdImg = new Regex(@"!\[.*?\]\((.*?)\)");
			clean = mdImg.Replace(clean, "<img src=\"$1\" />");

            return clean;
        }

		async Task FixData()
		{
            const string BACKUP_DIR = "backup";
            var pages = await lnDb.Pages.Paginate(SERIES_ID, 1, 99999999);
            if (pages is null || pages.Count == 0)
            {
                _logger.LogError("No pages found for series: {id}", SERIES_ID);
                return;
            }

            var backupDir = Path.Combine(BACKUP_DIR, $"{SERIES_ID}_{DateTime.Now:yyyy-MM-dd-HH-mm}");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            foreach (var page in pages.Results)
            {
                var path = Path.Combine(backupDir, $"{page.Id}.html");
                await File.WriteAllTextAsync(path, page.Content);

                var clean = Cleanse(page.Content);
                page.Content = clean;

                await lnDb.Pages.Update(page);
                _logger.LogInformation("Updated page: {id}", page.Id);
            }
        }

		async Task PrintImages()
		{
            var pages = await lnDb.Pages.Paginate(SERIES_ID, 1, 99999999);
			if (pages is null || pages.Count == 0)
			{
				_logger.LogError("No pages found for series: {id}", SERIES_ID);
				return;
			}

			var images = new HashSet<string>();

            var img = new Regex(@"<img.*?src=""(.*?)"".*?>");
			foreach (var page in pages.Results)
			{
				var matches = img.Matches(page.Content);
				foreach (System.Text.RegularExpressions.Match match in matches)
				{
					var src = match.Groups[1].Value;
					images.Add(src.Split('?').First().Trim());
					_logger.LogInformation("Image: {src}", src);
				}
			}

            await File.WriteAllLinesAsync("images.txt", images);

			var client = new HttpClient();

            foreach (var image in images)
			{
				var name = image.Split('/').Last();
                var path = Path.Combine(IMAGE_OUTPUT, name);

				using var req = new HttpRequestMessage(HttpMethod.Get, image);
                using var res = await client.SendAsync(req);
				res.EnsureSuccessStatusCode();
				using var io = File.Create(path);
				using var str = await res.Content.ReadAsStreamAsync();
                await str.CopyToAsync(io);
				await io.FlushAsync();

				_logger.LogInformation("Downloaded: {name}", name);
            }
        }

		async Task FixImages()
        {
            var pages = await lnDb.Pages.Paginate(SERIES_ID, 1, 99999999);
            if (pages is null || pages.Count == 0)
            {
                _logger.LogError("No pages found for series: {id}", SERIES_ID);
                return;
            }

            var img = new Regex(@"<img.*?src=""(.*?)"".*?>");
            foreach (var page in pages.Results)
            {
                var matches = img.Matches(page.Content);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var src = match.Groups[1].Value;
                    var url = src.Split('?').First().Trim();
					var name = url.Split('/').Last();

					var path = Path.Combine(IMAGE_OUTPUT, name);
                    if (!File.Exists(path))
                    {
                        _logger.LogInformation("Image not found: {name}", name);
                        continue;
                    }

                    page.Content = page.Content.Replace(src, "file://" + path);
                    _logger.LogInformation("Replaced: {name}", name);
                }

				await lnDb.Pages.Update(page);
                _logger.LogInformation("Updated page: {id}", page.Id);
            }

        }

		await FixImages();
    }

	public async Task Manganato()
	{
		const string URL = "https://www.natomanga.com/manga-zx1002932";
		var (matches, id) = manganato.MatchesProvider(URL);
		if (!matches || string.IsNullOrEmpty(id))
		{
            _logger.LogError("Failed to match provider");
            return;
        }

		var items = await manganato.Manga(id);
		if (items == null)
		{
			_logger.LogError("Failed to fetch manga");
			return;
		}

		var chapter = items.Chapters.FirstOrDefault();
		if (string.IsNullOrEmpty(chapter?.Url))
		{
			_logger.LogInformation("Could not get chapter URL");
			return;
		}

		var pages = await manganato.ChapterPages(chapter.Url);

        _logger.LogInformation("Found");
    }


    public async Task Vampiramtl()
	{
        async Task Info()
        {
            const string URL = "https://www.vampiramtl.com/tgs/";
            var info = await vampiramtl.GetSeriesInfo(URL);
            if (info is null)
            {
                _logger.LogError("Failed to fetch series info");
                return;
            }

            _logger.LogInformation("Title: {Title}", info.Title);
        }

        async Task Chapter()
        {
            const string URL = "https://www.vampiramtl.com/tgs/v1-illustrations/";
            var chap = await vampiramtl.GetChapter(URL, string.Empty);
            if (chap is null)
            {
                _logger.LogError("Failed to fetch chapter");
                return;
            }
            _logger.LogInformation("Chapter: {Title}", chap.ChapterTitle);
        }

        async Task Volumes()
        {
            const string URL = "https://www.vampiramtl.com/tgs/";
            var info = await vampiramtl.Volumes(URL).ToArrayAsync();
            if (info.Length == 0)
            {
                _logger.LogError("Failed to fetch volumes");
                return;
            }

            foreach (var vol in info)
            {
                _logger.LogInformation("Volume: {Title}", vol.Title);
                foreach (var chap in vol.Chapters)
                    _logger.LogInformation("\tChapter: {Title}", chap.Title);
            }
        }

        await Volumes();
        await Info();
        await Chapter();
    }

	public async Task MagicHouse()
	{
		async Task Info()
		{
			const string URL = "https://magichousetldotcom.wordpress.com/my-heart-is-that-of-an-uncle/";
			var info = await magicHouse.GetSeriesInfo(URL);
			if (info is null)
			{
				_logger.LogError("Failed to fetch series info");
				return;
			}

			_logger.LogInformation("Title: {Title}", info.Title);
		}

		async Task Chapter()
		{
			const string URL = "https://magichousetldotcom.wordpress.com/2024/06/03/c1-my-heart-is-that-of-an-uncle/";
			var chap = await magicHouse.GetChapter(URL, string.Empty);
			if (chap is null)
			{
				_logger.LogError("Failed to fetch chapter");
				return;
			}
			_logger.LogInformation("Chapter: {Title}", chap.ChapterTitle);
		}

		async Task Volumes()
        {
            const string URL = "https://magichousetldotcom.wordpress.com/my-heart-is-that-of-an-uncle/";
			var info = await magicHouse.Volumes(URL).ToArrayAsync();
			if (info.Length == 0)
			{
                _logger.LogError("Failed to fetch volumes");
                return;
            }

            foreach (var vol in info)
            {
                _logger.LogInformation("Volume: {Title}", vol.Title);
                foreach (var chap in vol.Chapters)
                    _logger.LogInformation("\tChapter: {Title}", chap.Title);
            }
        }

		await Volumes();
        await Info();
		await Chapter();
	}


    public async Task HeadCanon()
	{
		async Task Info()
        {
            const string URL = "https://headcanontl.wordpress.com/all-works-maid-table-of-contents/";
            var (info, vols) = await headCanon.Actual(URL);

            if (info is null ||
                vols.Length == 0)
            {
                _logger.LogError("Error fetching series info");
                return;
            }

            _logger.LogInformation("Title: {Title}", info.Title);
        }

		async Task Chapter()
		{
			const string URL = "https://headcanontl.wordpress.com/2022/04/30/all-works-maid-vol1-illustrations/";
			var chap = await headCanon.GetChapter(URL, string.Empty);

			if (chap is null)
			{
                _logger.LogError("Error fetching chapter");
                return;
            }

			_logger.LogInformation("Chapter: {Title}", chap.ChapterTitle);
		}

		await Chapter();
	}

	public async Task Ftl()
	{
		const string URL = "https://fanstranslations.com/novel/why-am-i-a-priestess-when-i-reach-the-maximum-level/";

        async Task GetVolumes()
        {
            var volumes = ftl.Volumes(URL);

            await foreach (var volume in volumes)
            {
                _logger.LogInformation("Volume: {Title} - {Url}", volume.Title, volume.Url);
                foreach (var chapter in volume.Chapters)
                {
                    _logger.LogInformation("\t\tChapter: {Title} - {Url}", chapter.Title, chapter.Url);
                }
            }
        }

        async Task GetInfo()
		{
            var info = await ftl.GetSeriesInfo(URL);
            if (info is null)
            {
                _logger.LogError("Failed to fetch series info");
                return;
            }

            _logger.LogInformation("Title: {Title}", info.Title);
        }

		async Task GetChapter()
		{
			const string CHAP_URL = "https://fanstranslations.com/novel/why-am-i-a-priestess-when-i-reach-the-maximum-level/vol-3-chapter-46/";
			var chapter = await ftl.GetChapter(CHAP_URL, "");
			if (chapter is null)
			{
				_logger.LogError("Failed to fetch chapter");
				return;
			}

			_logger.LogInformation("Chapter: {Title}", chapter.ChapterTitle);
        }

		//await GetInfo();
		//await GetVolumes();
		await GetChapter();
	}

	public async Task Baka()
	{
		const string URL = "https://bakapervert.wordpress.com/arifureta-shokugyo-de-sekai-saikyou/";

		async Task GetVolumes()
		{
            var volumes = baka.Volumes(URL);

            await foreach (var volume in volumes)
            {
                _logger.LogInformation("Volume: {Title} - {Url}", volume.Title, volume.Url);
                foreach (var chapter in volume.Chapters)
                {
                    _logger.LogInformation("\tChapter: {Title} - {Url}", chapter.Title, chapter.Url);
                }
            }
        }
		
		async Task GetInfo()
		{
			var info = await baka.GetSeriesInfo(URL);
			if (info is null)
			{
				_logger.LogError("Failed to fetch series info");
				return;
			}

			_logger.LogInformation("Title: {Title}", info.Title);
		}

		await GetInfo();
		await GetVolumes();
    }

	public async Task Crunchy()
	{
		const string token = "";

		var data = await crunchy.All(token).ToListAsync();
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
			await db.Upsert(anime);

		_logger.LogInformation("Finsihed loading crunchyroll anime");
	}

	public async Task Hidive()
	{
		var data = await hidive.Fetch("https://www.hidive.com/movies/", "movie").ToArrayAsync();
		//using var io = File.OpenWrite("hidive.json");
		//await JsonSerializer.SerializeAsync(io, data);
		await mongo.Upsert(data);
	}

	public async Task Test()
	{
		await new[] { 47, 50, 49 }
			.Select(t => lnDb.Series.Delete(t))
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

		await mongo.Upsert(data);

		_logger.LogInformation("Data loaded");
	}

	public async Task FetchFunimationResources()
	{
		var data = await fun.All().ToListAsync();
		using var io = File.OpenWrite(FUN_JSON);
		await JsonSerializer.SerializeAsync(io, data);
	}

	public async Task DetermineImageSizes()
	{
		var dic = new Dictionary<string, List<(int width, int height, string source)>>();
		var data = await fun.All().ToListAsync();

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

				using var res = await api.Create(im.Source, _json, "GET").Result();
				if (res == null || !res.IsSuccessStatusCode)
				{
					_logger.LogError("Failed to fetch resource: {source}", im.Source);
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
		var services = new IAnimeApiService[] { vrv, fun };

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
		var data = await vrv.All().ToListAsync();
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
			var resources = await vrv.Fetch(op.ToString());
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
		var data = await db.All();
		if (data == null)
		{
			_logger.LogError("Data is null");
			return;
		}

		foreach (var anime in data)
			await db.Upsert(anime.Clean());
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

		var all = await mongo.All(1, 9000);

		foreach(var a in all.Results)
			await db.Upsert(convertAnime(a));
	}

	public async Task LoadLightNovel()
	{
		const string FIRST_CHAPTER = "";
		const int SRC = 1;

		var src = ln.Sources()[SRC];

		var chaps = src.DbChapters(FIRST_CHAPTER);

		if (chaps == null)
		{
			_logger.LogError("No chapters found to load!");
			return;
		}

		await foreach (var chap in chaps)
			await chapDb.Upsert(chap);

		_logger.LogInformation("Book uploaded!");
	}

	public async Task ToPdf()
	{
		const string ID = "445C5E7AC91435D2155BC1D1DAAE8EB8";
		await pdf.ToPdf(ID);
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
		var (epubs, dir) = await ln.GenerateEpubs(BOOK_ID, settings);

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
		var src = ln.Sources()[SOURCE_ID];
		var book = await chapDb.BookById(ID);
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
			await chapDb.Upsert(item);
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
		var (_, books) = await chapDb.Books();

		await Task.WhenAll(books.Select(MigrateBook));
	}

	public async Task MigrateBook(DbBook book)
	{
		try
		{
			var src = ln.Source(book.LastChapterUrl);
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

			var id = await lnDb.Series.Upsert(new Series
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
			var (_, chaps) = await chapDb.Chapters(book.Id, 1, 10000);

			foreach (var chap in chaps)
			{
				await lnDb.Pages.Upsert(new Page
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
		var scaffold = await lnDb.Series.Scaffold(SERIES_ID);

		const string JM_IMG_DIR = @"C:\Users\Cardboard\Desktop\JM";
		const string JM_IMG_URL = "https://static.index-0.com/jm/volumeart";
		var cvi = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Cover.jpg", $"{JM_IMG_URL}/Vol{volume}/Cover.jpg");
		var coi = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Contents.jpg", $"{JM_IMG_URL}/Vol{volume}/Contents.jpg");
		var ini = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Inserts", $"{JM_IMG_URL}/Vol{volume}/Inserts");
		var frd = (int volume) => ($"{JM_IMG_DIR}\\Vol{volume}\\Forwards", $"{JM_IMG_URL}/Vol{volume}/Forwards");
		var toUris = ((string dir, string url) a) => Directory.GetFiles(a.dir).Select(t => $"{a.url}/{Path.GetFileName(t)}").ToArray();

		
		var series = await lnDb.Series.Fetch(SERIES_ID);
		var (_, _, pages) = await lnDb.Pages.Paginate(SERIES_ID, 1, 1000);

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

		await lnDb.Series.Upsert(series);

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

			var id = await lnDb.Books.Upsert(new Book
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
				var chapId = await lnDb.Chapters.Upsert(new LightNovel.Core.Chapter
				{
					HashId = $"{page.Title}-{i}-{p}".MD5Hash(),
					Title = page.Title,
					Ordinal = p,
					BookId = id
				});
				await lnDb.ChapterPages.Upsert(new ChapterPage
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
		var item = await mangaDex.Search("sonna hiroki");

		Console.WriteLine($"Manga: {item?.Data?.Count}");
	}

	public async Task TestMangaClash()
	{
		const string id = "402922",
			chapId = "chapter-118";

		var manga = await nhentai.Manga(id);
		var chap = await nhentai.ChapterPages(id, chapId);

		Console.WriteLine("Manga Source: " + manga?.Title);
	}

	public async Task TestManga()
	{
		var urls = new[]
		{
			"https://mangakakalot.com/read-rm5iu158524511364",
			"https://mangakakalot.com/manga/sekai_saikyou_no_shinjuu_tsukai"
		};

		foreach (var url in urls)
		{
			var manga = await _manga.Manga(url, null, true);
			if (manga == null)
			{
				_logger.LogWarning("Error occurred while fetching manga");
				return;
			}

			var chap = manga.Chapters.First();
			var chapter = await _manga.MangaPages(chap, true);

			if (chapter.Length == 0)
			{
				_logger.LogWarning("Error occurred while fetching pages");
				continue;
			}

			_logger.LogInformation("Fetched all of the manga");
		}
	}

	public async Task Index()
	{
		await match.IndexLatest();
	}

	public async Task PolyfillChapters()
	{
		var chunkMethod = async (MangaProgress[] data, int tid) =>
		{
			_logger.LogInformation($"[{tid}] >> Started indexing chunk of: {data.Length}");

			int count = 0;
			int reportAt = data.Length / 100;
			int beforeTimeout = 0;
			int timeoutAfter = 39;
			foreach (var m in data)
			{
				if (m.Manga.Provider != "mangadex") continue;

				count++;
				var mwc = await _manga.Manga(m.Manga.Id, null);
				if (mwc == null)
				{
					_logger.LogWarning($"[{tid}] >> Couldn't find manga chapters... This shouldn't happen and is a bail out :: {m.Manga.Id}");
					continue;
				}

				var chapters = mwc.Chapters;
				foreach (var chapter in chapters)
				{
					var (worked, requiresTimeout) = await _manga.IndexChapter(mwc.Manga, chapter);

					if (!worked)
						_logger.LogWarning($"[{tid}] >> Failed to index entire chapter for: {mwc.Manga.Id} >> {chapter.Id}");

					if (!requiresTimeout) continue;

					beforeTimeout++;
					if (beforeTimeout < timeoutAfter) continue;

					_logger.LogInformation($"[{tid}] >> taking a break.");
					await Task.Delay(1000 * 60);
					_logger.LogInformation($"[{tid}] >> break finished.");
					beforeTimeout = 0;
				}

				_logger.LogInformation($"[{tid}] >> Finished indexing: {m.Manga.Title}");

				if (count % reportAt == 0)
					_logger.LogInformation($"[{tid}] >> Progress: {count / (decimal)data.Length * 100:0.00}%");
			}

			_logger.LogInformation($"[{tid}] >> Finished processing all manga!");
		};

		var all = await _manga.All();

		if (all.Results.Length == 0)
		{
			_logger.LogWarning("Couldn't find any manga? Did you fuck up again?");
			return;
		}

		_logger.LogInformation($"Starting Manga Indexing: {all.Results.Length}");

		//var chunks = all.Results
		//	.Split(3)
		//	.ToArray();

		await chunkMethod(all.Results, 0);
		//var rnd = new Random();
		//await Parallel.ForEachAsync(chunks, (t, _) => ProcessChunk(t, rnd.Next(0, 500)));
		_logger.LogInformation("Finished!");
	}

	public async Task FixCache()
	{
		var manga = await cacheDb.All();
		var ids = manga.Select(t => t.SourceId).ToArray();

		var md = await mangaDex.AllManga(ids);
		if (md == null || md.Data.Count == 0)
		{
			_logger.LogError("No results from mangadex");
			return;
		}

		foreach(var m in manga)
		{
			var om = md.Data.FirstOrDefault(t => t.Id == m.SourceId);
			if (om == null)
			{
				_logger.LogWarning($"Could not find MD manga for: {m.Title}");
				continue;
			}

			var coverFile = (om.Relationships.FirstOrDefault(t => t is CoverArtRelationship) as CoverArtRelationship)?.Attributes?.FileName;
			var coverUrl = $"https://mangadex.org/covers/{m.SourceId}/{coverFile}";

			m.Cover = coverUrl;
			await cacheDb.Upsert(m);
		}

		_logger.LogInformation("Cover art has been fixed");
	}

	public async Task IndexManga()
	{
		const string SOURCE_ID = "5a5651fe-7a1a-4b85-a812-3bf3cf72eb0c";
		await match.IndexManga(SOURCE_ID);
		_logger.LogInformation("Finished");
	}

	public async Task IndexDbImages()
	{
		var manga = await mangaDb.All();
		var chapters = await mangaDb.AllChapters();

		int count = 0;
		foreach(var m in manga)
		{
			if (count >= 10)
			{
				count = 0;
				_logger.LogInformation("Delaying for 5 seconds to mitigate rate-limits");
				await Task.Delay(5 * 1000);
			}

			_logger.LogInformation($"Indexing pages for: {m.Title} ({m.Id})");
			var chaps = chapters.Where(t => t.MangaId == m.Id);
			var cmi = await cacheDb.Upsert(m);
			foreach(var chapter in chaps)
			{
				chapter.MangaId = cmi;
				await cacheDb.Upsert(chapter);

				var chunk = chapter.Pages.Select((t, i) => (t, i)).Split(5);
				await Parallel.ForEachAsync(chunk, async (t, c) =>
				{
					foreach(var (url, index) in t)
					{
						var meta = new MangaMetadata
						{
							Id = url.MD5Hash(),
							Source = m.Provider,
							Url = url,
							Type = MangaMetadataType.Page,
							MangaId = m.SourceId,
							ChapterId = chapter.SourceId,
							Page = index + 1,
						};

						await match.IndexPageProxy(url, meta, m.Referer);
					}
				});
			}

			_logger.LogInformation($"Finished indexing pages for: {m.Title} ({cmi}).");
			count++;
		}

		_logger.LogInformation("Finished");
	}

	public async Task IndexCovers()
	{
		var manga = await cacheDb.All();
		var images = manga.Select(t => (t.Referer, new MangaMetadata
		{
			Id = t.Cover.MD5Hash(),
			Source = t.Provider,
			Url = t.Cover,
			Type = MangaMetadataType.Cover,
			MangaId = t.SourceId,
		}));

		_logger.LogInformation("Starting cover indexing for: " + manga.Length);
		foreach (var (referer, data) in images)
			await match.IndexPageProxy(data.Url, data, referer);
		_logger.LogInformation("Finished");
	}

	public async Task TestBattow()
	{
		var manga = await battwo.Manga("113585");

		Console.WriteLine("Found");
	}

	public async Task FixTags()
	{
		var mangas = await mangaDb.Search(new Core.Models.MangaFilter
		{
			Size = 10000,
			Sources = new[] { "nhentai" },
			Nsfw = NsfwCheck.DontCare,
			Page = 1,
			State = TouchedState.All
		}, null, true);

		if (mangas == null || mangas.Results.Length == 0)
		{
			_logger.LogError("No manga found for search");
			return;
		}

		foreach(var manga in mangas.Results)
		{
			manga.Manga.Tags = manga.Manga.Tags.Select(t =>
			{
				return Regex.Replace(t, @"[\d-]", string.Empty).Replace("\r", "").Replace("\n", "").Trim();
			}).Where(t => !string.IsNullOrEmpty(t)).ToArray();
			await mangaDb.Upsert(manga.Manga);
		}

		_logger.LogInformation("Tags fixed");
	}

	public async Task FixProgress()
	{
		var progresses = await mangaDb.AllProgress();
		if (progresses.Length == 0)
		{
			_logger.LogError("No progresses found.");
			return;
		}

		var cache = new Dictionary<long, MangaWithChapters>();

		async Task<MangaWithChapters?> getCache(long id)
		{
			if (cache.ContainsKey(id)) return cache[id];

			var prog = await mangaDb.GetManga(id, null);
			if (prog == null) return null;

			cache.Add(id, prog);
			return prog;
		}

		foreach (var prog in progresses)
		{
			var manga = await getCache(prog.MangaId);
			if (manga == null)
			{
				_logger.LogWarning("Couldnt find manga for: {MangaId}", prog.MangaId);
				continue;
			}

			var pages = new List<DbMangaChapterProgress>();

			var read = manga.Chapters.FirstOrDefault(t => t.Id == prog.MangaChapterId);

			if (read == null)
			{
				_logger.LogWarning("Couldn't find read chapter for progress: {Id}", prog.Id);
				continue;
			}

			foreach(var chap in manga.Chapters)
			{
				if (chap.Ordinal > read.Ordinal) continue;

				if (chap.Ordinal < read.Ordinal)
				{
					pages.Add(new DbMangaChapterProgress
					{
						ChapterId = chap.Id,
						PageIndex = chap.Pages.Length
					});
					continue;
				}

				pages.Add(new DbMangaChapterProgress { ChapterId = chap.Id, PageIndex = prog.PageIndex ?? 0 });
			}

			if (pages.Count == 0) continue;

			prog.Read = pages.ToArray();
			await mangaDb.UpdateProgress(prog);
			_logger.LogInformation("Finished: User: {ProfileId}, Manga Id: {MangaId}, Prog: {Id} - {Count}", prog.ProfileId, prog.MangaId, prog.Id, pages.Count);
		}

		_logger.LogInformation("Finished with: {Length}", progresses.Length);
	}

	public async Task TestLnt()
	{
		var url = "https://lightnovelstranslations.com/novel/i-woke-up-piloting-the-strongest-starship-so-i-became-a-space-mercenary/";
		var info = lnt.Volumes(url);

		var vols = await info.ToArrayAsync();
		foreach(var volume in vols)
		{
			_logger.LogInformation("Found Volume: {title}", volume.Title);
		}

		var chapUrl = "https://lightnovelstranslations.com/novel/i-woke-up-piloting-the-strongest-starship-so-i-became-a-space-mercenary/415-pirate-hunting/";

		var chapter = await lnt.GetChapter(chapUrl, "");
		_logger.LogInformation("Finished");
	}

	public async Task TestNUChapters()
	{
		var url = "https://www.novelupdates.com/series/tsuki-ga-michibiku-isekai-douchuu/";

		var (info, chaps) = await _info.GetChapters(url);

		if (info == null || 
			chaps.Length == 0)
		{
			_logger.LogWarning("Couldn't find content for the given NU page.");
			return;
		}
	}

	public async Task LntImageFix()
	{
		var pages = await lnDb.Pages.ImagePages();

		if (pages.Length == 0)
		{
            _logger.LogWarning("No pages found.");
            return;
        }

		foreach(var page in pages)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(page.Content);

			foreach(var child in doc.DocumentNode.ChildNodes.ToArray())
			{
				if (child.InnerHtml.ToLower().Contains("<img"))
					child.CleanupNode();
			}

			page.Content = doc.DocumentNode.InnerHtml;

			await lnDb.Pages.Update(page);
		}
	}

	public async Task PurgeAll()
	{
        var pages = await lnDb.Pages.AnchorPages();

        foreach (var page in pages)
        {
            page.Content = purge.PurgeBadElements(page.Content);
            await lnDb.Pages.Update(page);
        }

        _logger.LogInformation("Finished");
    }

	public async Task PurgeBadStuff()
	{
        var page = await lnDb.Pages.Fetch(577);
		if (page == null)
		{
			_logger.LogInformation("Page does not exist");
			return;
		}

		var before = page.Content;
		var after = purge.PurgeBadElements(page.Content);

		_logger.LogInformation("Before: {before}\r\nAfter: {after}", before, after);
    }

	public async Task Nyx()
	{
		await PurgeAll();

        var pages = await lnDb.Pages.AnchorPages();

		var lanks = new List<(string href, string content, Page page)>();

		foreach(var page in pages)
		{
			var doc = new HtmlDocument();
			doc.LoadHtml(page.Content);

			var links = doc.DocumentNode.SelectNodes("//a")
				.Select(t => (t.GetAttributeValue("href", ""), t.InnerText))
				.ToArray();

			lanks.AddRange(links.Select(t => (t.Item1, t.InnerText, page)));
		}

		var csv = lanks
            .Select(t => new NyxCsvLine(t.href,
				t.content,
				t.page.Id.ToString(),
				t.page.Title,
				t.page.HashId,
				t.page.SeriesId.ToString())
			)
            .ToArray();
		var distinct = lanks.Select(t => t.Item1).Distinct().ToArray();

		_logger.LogInformation("Links: {Count}", distinct.Length);

		//await _lnDb.Series.Delete(69);

		//var chap = "https://nyx-translation.com/2020/02/17/i-got-a-cheat-ability-in-a-different-world-and-become-extraordinary-even-in-the-real-world-chapter-1-part-1/";
		//var chapData = await _nyx.GetChapter(chap, "");


  //      var url = "https://nyx-translation.com/i-got-a-cheat-ability-in-a-different-world-and-become-extraordinary-even-in-the-real-world/";
		//var info = await _nyx.Volumes(url).ToArrayAsync();

		//_logger.LogInformation("Found info: ");
	}

	public async Task ZirusTest()
	{
		var url = "https://zirusmusings.net/series/mg";
		var info = await zirus.GetSeriesInfo(url);
		if (info == null)
		{
			_logger.LogInformation("Could not find series.");
			return;
		}

		_logger.LogInformation("Series: {title}", info.Title);

		url = "https://zirusmusings.net/_next/data/2XCJ9PL_uxx4mf-pTDI_k/series/mg/1/0.json?seriesId=mg&firstId=1&secondId=0";

		//var chapters = _zirus.Chapters(url);
		//await foreach(var chapter in chapters)
		//{
		//	_logger.LogInformation("Chapter: {title}", chapter.ChapterTitle);
		//}
		var chap = await zirus.GetChapter(url, "");
		if (chap == null)
		{
			_logger.LogInformation("Could not find series.");
			return;
		}

		_logger.LogInformation("Series: {title}", chap.ChapterTitle);

	}

	public async Task Fetishes()
	{
		const string filePath = "images.json";
		const string OUTPUT = "fetish-images";

		using var io = File.OpenRead(filePath);
		var urls = await JsonSerializer.DeserializeAsync<string[]>(io) ?? Array.Empty<string>();

		if (!Directory.Exists(OUTPUT)) Directory.CreateDirectory(OUTPUT);

		foreach(var url in urls)
		{
			var filename = url.Split('/').Last().Replace("png.png", ".png");
            _logger.LogInformation("Writing: {filename}", filename);
            var (data, _, _, _) = await api.GetData(url);

			var path = Path.Combine(OUTPUT, filename);
			using var write = File.OpenWrite(path);
			await data.CopyToAsync(write);
			await write.FlushAsync();
			_logger.LogInformation("Finished Writing {filename}", filename);
        }
	}

	public async Task NnconImages()
	{
		const string IMAGE_DIR = "nncon-images";

		if (!Directory.Exists(IMAGE_DIR)) Directory.CreateDirectory(IMAGE_DIR);

		var imageJson = @"[
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v1cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v1charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v1-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/nya-1024x732-1.gif"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v2cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v2charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v2-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v3cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v3charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v3-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v4cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v4charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v4-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v5cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v5charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v5-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v6cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v6charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v6-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v7.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v7char.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v7-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v8cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v8charlist-2048x1503-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v8-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v9.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v9_char-2048x1503-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v9_1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v10.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v10char-2048x1503-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/06/v10_1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/10/v11cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/10/v11charlist.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2021/10/v11_1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2022/10/v12cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2022/10/v12charlist-scaled.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2022/10/v12-1.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2022/10/v13cover.jpg"",
  ""https://novelonomicon.com/wp-content/uploads/2022/10/v13charlist-scaled.jpeg"",
  ""https://novelonomicon.com/wp-content/uploads/2022/10/v13-1.jpeg""
]";
		var images = JsonSerializer.Deserialize<string[]>(imageJson) ?? Array.Empty<string>();

		foreach(var image in images)
		{
            var filename = image.Split('/').Last();
            var (data, _, _, _) = await api.GetData(image);

            var path = Path.Combine(IMAGE_DIR, filename);
            using var write = File.OpenWrite(path);
            await data.CopyToAsync(write);
            await write.FlushAsync();
			await data.DisposeAsync();
            _logger.LogInformation("Finished Writing {filename}", filename);
        }
		_logger.LogInformation("Finished");
	}

	public async Task NnconLoad()
	{
		const string URL = "https://novelonomicon.com/novels/isekai-yururi-kikou/prologue/";
		var chapters = nnc.Chapters(URL);
		await foreach(var chap in chapters)
		{
			_logger.LogInformation("Chapter found: {0}", chap.ChapterTitle);
		}
	}

	public async Task FixYururiBooking()
    {
		static string plusOut(string input, string keep, char replacewith = '+')
		{
			var chars = input.ToCharArray();
			if (!input.Contains(keep))
				return new string(replacewith, input.Length);

			int i = 0;
			while(i <= chars.Length)
			{
				int index = input.IndexOf(keep, i);
				if (index == -1)
				{
					for(var x = i; x < chars.Length; x++)
						chars[x] = replacewith;
					break;
				}

				for(var x = i; x < index; x++)
					chars[x] = replacewith;
				i = index + keep.Length;
			}

			return new string(chars);
		}

		Console.WriteLine(plusOut("12xy34", "xy"));
		Console.WriteLine(plusOut("12xy34", "1"));
		Console.WriteLine(plusOut("12xy34xyabcxy", "xy"));
		Console.WriteLine(plusOut("abXYabcXYZ", "ab"));
		Console.WriteLine(plusOut("abXYabcXYZ", "abc"));

		return;



		const long SERIES_ID = 79;
        const string IMAGE_DIR = "nncon-images";
        const string IMAGE_URL = "https://static.index-0.com/image/nncon/";
		const string DEFAULT_IMAGE = "https://static.index-0.com/image/nncon/nya-1024x732-1.gif";

        var imageFiles = Directory.GetFiles(IMAGE_DIR).Select(path =>
        {
            var name = Path.GetFileName(path);
            return (path, name);
        }).ToArray();

        static IEnumerable<(int volume, Page[] pages)> SplitVolumes(Page[] pages, string[] splits)
		{
			int i = 0;
			var current = new List<Page>();
			foreach(var page in pages)
			{
				if (i >= splits.Length)
				{
                    current.Add(page);
                    continue;
                }

				var split = splits[i];
				if (page.Title == split)
				{
                    yield return (i + 1, current.ToArray());
                    current.Clear();
                    i++;
                }

                current.Add(page);
			}

			if (current.Count == 0) yield break;

			yield return (i + 1, current.ToArray());
		}

		string? GetImage(int number, string part, string? @default = null)
		{
			if (imageFiles == null || imageFiles.Length == 0) return @default;
			var mask = $"v{number}-{part}".ToLower();

			var valids = imageFiles.Where(t => t.name.ToLower().Contains(mask)).ToArray();
			if (valids.Length == 0) return @default;

			return $"{IMAGE_URL}{valids.First().name}";
		}

		string StripContent(string input)
		{
			return input;
		}

		Book FromVolume(Series series, int number)
		{
			var title = $"{series.Title} Vol {number}";
            var cover = GetImage(number, "cover", series.Image) ?? DEFAULT_IMAGE;
			string[] images = new[]
			{
				GetImage(number, "chars"),
				GetImage(number, "1"),
			}.Where(t => !string.IsNullOrEmpty(t))
				.ToArray()!;

			return new Book
			{
				SeriesId = series.Id,
				CoverImage = cover,
				Forwards = images,
				Inserts = images,
				Title = title,
				HashId = title.MD5Hash(),
				Ordinal = number
			};
		}

		Page FromPage(Series series, Page previous, long ordinal)
		{
			return new Page
			{
				HashId = previous.HashId,
				Title = previous.Title,
				Ordinal = ordinal,
				SeriesId = series.Id,
				Url = previous.Url,
				NextUrl = previous.NextUrl,
				Content = StripContent(previous.Content),
				Mimetype = previous.Mimetype,
			};
		}

        CChapter FromChapter(string title, int bc, long bid, long ordinal)
		{
            return new CChapter
            {
                HashId = $"{title}-{bc - 1}-{ordinal}".MD5Hash(),
				Title = title,
				Ordinal = ordinal,
				BookId = bid
            };
        }

		var api = (NovelApiService)napi;

        var scaffold = await lnDb.Series.Scaffold(SERIES_ID);
		if (scaffold == null)
		{
			_logger.LogWarning("Couldn't find series");
			return;
		}

		var series = scaffold.Series;

		var pages = scaffold.Books.SelectMany(b => b.Chapters.SelectMany(t => t.Pages)).Select(t => t.Page).ToArray();

		var splits = new[] 
		{ 
			"Chapter 28", "Chapter 60", "Chapter 95",  "Chapter 126", "Chapter 156", "Chapter 183",
			"Chapter 206", "V8 illustrations", "v9 illustrations", "v10 illustrations", "Chapter 305", "Chapter 333",
            "v12 & v13 illustrations", "v14 illustrations"
        };

		await lnDb.Series.Delete(SERIES_ID);

        series.Id = await lnDb.Series.Upsert(series);

		var books = SplitVolumes(pages, splits).ToArray();
		int pageOrdinal = 0;
		foreach(var (volume, ps) in books)
		{
			var bid = await lnDb.Books.Upsert(FromVolume(series, volume));
			int pageCount = 0;
			foreach(var page in ps)
			{
				if (page.Title.ToLower().Contains("illustrations")) continue;

				pageOrdinal++;
				pageCount++;

				var pid = await lnDb.Pages.Upsert(FromPage(series, page, pageOrdinal));
				var cid = await lnDb.Chapters.Upsert(FromChapter(page.Title, volume, bid, pageCount));
				await lnDb.ChapterPages.Upsert(new ChapterPage
				{
                    ChapterId = cid,
                    PageId = pid,
                    Ordinal = 0
                });
			}
		}
    }

	public async Task DownloadChapters()
	{
        const string URL = "https://rawkuma.com/manga/daikenja-no-manadeshi-bougyo-mahou-no-susume/";
		var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kuma-download");

		var fromHtml = true;

		var chapters = new List<(string path, int count)>();
		await foreach(var chapter in kuma.GetDownloadLinks(URL))
		{
			var zip = await (fromHtml 
				? kuma.DownloadFromPages(chapter, output) 
				: kuma.Download(chapter, output));
			if (string.IsNullOrEmpty(zip)) continue;

			if (fromHtml)
			{
				var fn = Path.GetFileNameWithoutExtension(zip);
                var ps = Directory.GetFiles(zip).Length;
                chapters.Add((fn, ps));
				continue;
            }

			zip = zip.Replace("\"", "");

			var fileName = Path.GetFileNameWithoutExtension(zip);
			var path = Path.Combine(output, fileName);
			if (Directory.Exists(path)) Directory.Delete(path, true);

			Directory.CreateDirectory(path);
			ZipFile.ExtractToDirectory(zip, path);

			var pages = Directory.GetFiles(path).Length;
			chapters.Add((fileName, pages));
		}

		foreach(var (path, count) in chapters)
		{
            _logger.LogInformation("Chapter {path} has {count} pages", path, count);
        }
	}

	public void CountFiles()
	{
        var output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "kuma-download");
		var dirs = Directory.GetDirectories(output);

		foreach(var dir in dirs)
		{
			var chapterName = dir.Split('\\').Last();
			var files = Directory.GetFiles(dir).Length;
			_logger.LogInformation("Chapter {chapterName} has {files} pages", chapterName, files);
		}
    }
}

public record class NyxCsvLine(string Link, string Content, string PageId, string Title, string HashId, string SeriesId);
