using System.Text.Json.Serialization;

namespace CardboardBox.Match.SauceNao;

public class Sauce
{
	[JsonPropertyName("header")]
	public SauceUser User { get; set; } = new();

	[JsonPropertyName("results")]
	public SauceResult[] Results { get; set; } = Array.Empty<SauceResult>();

	public class SauceUser
	{
		[JsonPropertyName("user_id")]
		public string? UserId { get; set; }

		[JsonPropertyName("account_type")]
		public string? AccountType { get; set; }

		[JsonPropertyName("short_limit")]
		public string ShortLimit { get; set; } = string.Empty;

		[JsonPropertyName("long_limit")]
		public string LongLimit { get; set; } = string.Empty;

		[JsonPropertyName("long_remaining")]
		public int LongRemaining { get; set; }

		[JsonPropertyName("short_remaining")]
		public int ShortRemaining { get; set; }

		[JsonPropertyName("status")]
		public int Status { get; set; }

		[JsonPropertyName("results_requested")]
		public int ResultsRequested { get; set; }

		[JsonPropertyName("index")]
		public Dictionary<string, SauceDatabase> Index { get; set; } = new();

		[JsonPropertyName("search_depth")]
		public string SearchDepth { get; set; } = string.Empty;

		[JsonPropertyName("minimum_similarity")]
		public double MinimumSimilarity { get; set; }

		[JsonPropertyName("query_image_display")]
		public string QueryImageDisplay { get; set; } = string.Empty;

		[JsonPropertyName("query_image")]
		public string QueryImage { get; set; } = string.Empty;

		[JsonPropertyName("results_returned")]
		public int ResultsReturned { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = string.Empty;
	}

	public class SauceDatabase
	{
		[JsonPropertyName("status")]
		public int Status { get; set; }

		[JsonPropertyName("parent_id")]
		public int ParentId { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("results")]
		public int? Results { get; set; }
	}

	public class SauceHeader
	{
		[JsonPropertyName("similarity")]
		public string Similarity { get; set; } = string.Empty;

		[JsonPropertyName("thumbnail")]
		public string Thumbnail { get; set; } = string.Empty;

		[JsonPropertyName("index_id")]
		public int IndexId { get; set; }

		[JsonPropertyName("index_name")]
		public string IndexName { get; set; } = string.Empty;
	}

	public class SauceData
	{
		[JsonPropertyName("ext_urls")]
		public string[] ExternalUrls { get; set; } = Array.Empty<string>();

		[JsonPropertyName("title")]
		public string? Title { get; set; }

		[JsonPropertyName("author_name")]
		public string? AuthorName { get; set; }

		[JsonPropertyName("author_url")]
		public string? AuthorUrl { get; set; }

		[JsonPropertyName("pixiv_id")]
		public int? PixivId { get; set; }

		[JsonPropertyName("member_name")]
		public string? MemberName { get; set; }

		[JsonPropertyName("member_id")]
		public int? MemberId { get; set; }

		[JsonPropertyName("bcy_id")]
		public int? bcyId { get; set; }

		[JsonPropertyName("member_link_id")]
		public int? MemberLinkId { get; set; }

		[JsonPropertyName("bcy_type")]
		public string? BcyType { get; set; }

		[JsonPropertyName("created_at")]
		public DateTime? CreatedAt { get; set; }

		[JsonPropertyName("pawoo_id")]
		public int? PawooId { get; set; }

		[JsonPropertyName("pawoo_user_acct")]
		public string? PawooUserAccount { get; set; }

		[JsonPropertyName("pawoo_user_username")]
		public string? PawooUsername { get; set; }

		[JsonPropertyName("pawoo_user_display_name")]
		public string? PawooDisplayName { get; set; }

		[JsonPropertyName("anidb_aid")]
		public int? AnimeId { get; set; }

		[JsonPropertyName("source")]
		public string? Source { get; set; }

		[JsonPropertyName("part")]
		public string? Part { get; set; }

		[JsonPropertyName("year")]
		public string? Year { get; set; }

		[JsonPropertyName("est_time")]
		public string? EstTime { get; set; }

		[JsonPropertyName("seiga_id")]
		public int? SeigaId { get; set; }

		[JsonPropertyName("sankaku_id")]
		public int? SankakuId { get; set; }

		[JsonPropertyName("danbooru_id")]
		public int? DanbooruId { get; set; }

		[JsonPropertyName("company")]
		public string? Company { get; set; }

		[JsonPropertyName("getchu_id")]
		public string? GetchuId { get; set; }

		[JsonPropertyName("md_id")]
		public string? MangaDexId { get; set; }
	}

	public class SauceResult
	{
		[JsonPropertyName("header")]
		public SauceHeader Header { get; set; } = new();

		[JsonPropertyName("data")]
		public SauceData Data { get; set; } = new();
	}
}

public enum SauceNaoDatabase : int
{
	HMagazines = 0,
	HGameCG = 2,
	TheDoujinshiAndMangaLexicon = 3,
	Pixiv1 = 4,
	Pixiv2 = 5,
	Pixiv3 = 51,
	Pixiv4 = 52,
	Pixiv5 = 6,
	Unknown = 7,
	NicoNicoSeiga = 8,
	Danbooru = 9,
	drawr = 10,
	Nijite = 11,
	Yandere = 12,
	Openingsmoe = 13,
	Shuttershock = 15,
	FAKKU = 16,
	nHentai = 18,
	Market2D = 19,
	MediBang = 20,
	AniDb = 21,
	AniDb2 = 211,
	HAnime = 22,
	IMDb = 23,
	Shows = 24,
	Gelbooru = 25,
	Konachan = 26,
	SankakuChannel = 27,
	AnimePictures = 28,
	e621 = 29,
	SankakuChannel2 = 30,
	bcynet = 31,
	bcynet2 = 32,
	PortalGraphics = 33,
	deviantArt = 34,
	Pawoo = 35,
	Madokami = 36,
	MangaDex = 37,
	Ehentai = 38,
	ArtStation = 39,
	FurAffinity = 40,
	Twitter = 41,
	FurryNetwork = 42
}
