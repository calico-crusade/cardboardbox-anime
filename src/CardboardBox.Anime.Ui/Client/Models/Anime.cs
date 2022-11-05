namespace CardboardBox.Anime.Ui.Client.Models
{
	public class AnimeModel : DbObject
	{
		[JsonPropertyName("hashId")]
		public string HashId { get; set; } = "";

		[JsonPropertyName("animeId")]
		public string AnimeId { get; set; } = "";

		[JsonPropertyName("link")]
		public string Link { get; set; } = "";

		[JsonPropertyName("title")]
		public string Title { get; set; } = "";

		[JsonPropertyName("description")]
		public string Description { get; set; } = "";

		[JsonPropertyName("platformId")]
		public string PlatformId { get; set; } = "";

		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("mature")]
		public bool Mature { get; set; } = false;

		[JsonPropertyName("languages")]
		public string[] Languages { get; set; } = Array.Empty<string>();

		[JsonPropertyName("languageTypes")]
		public string[] LanguageTypes { get; set; } = Array.Empty<string>();

		[JsonPropertyName("ratings")]
		public string[] Ratings { get; set; } = Array.Empty<string>();

		[JsonPropertyName("tags")]
		public string[] Tags { get; set; } = Array.Empty<string>();

		[JsonPropertyName("images")]
		public Image[] Images { get; set; } = Array.Empty<Image>();

		[JsonPropertyName("ext")]
		public Extension[] Ext { get; set; } = Array.Empty<Extension>();

		[JsonPropertyName("otherPlatforms")]
		public List<AnimeModel> OtherPlatforms { get; set; } = new();
	}

	public class Image
	{
		[JsonPropertyName("width")]
		public int? Width { get; set; }

		[JsonPropertyName("height")]
		public int? Height { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("source")]
		public string Source { get; set; } = "";

		[JsonPropertyName("platformId")]
		public string PlatformId { get; set; } = "";
	}

	public class Extension
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = "";

		[JsonPropertyName("value")]
		public string Value { get; set; } = "";
	}

	public class Filter
	{
		[JsonPropertyName("key")]
		public string Key { get; set; } = string.Empty;

		[JsonPropertyName("values")]
		public string[] Values { get; set; } = Array.Empty<string>();
	}

	public class PaginatedResult<T>
	{
		[JsonPropertyName("pages")]
		public long Pages { get; set; }

		[JsonPropertyName("count")]
		public long Count { get; set; }

		[JsonPropertyName("results")]
		public T[] Results { get; set; } = Array.Empty<T>();

		public PaginatedResult() { }

		public PaginatedResult(int pages, int count, T[] results)
		{
			Pages = pages;
			Count = count;
			Results = results;
		}

		public PaginatedResult(long pages, long count, T[] results)
		{
			Pages = pages;
			Count = count;
			Results = results;
		}

		public void Deconstruct(out long pages, out long count, out T[] results)
		{
			pages = Pages;
			count = Count;
			results = Results;
		}
	}
}
