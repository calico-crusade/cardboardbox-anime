namespace CardboardBox.Manga.MangaDex.Models
{
	public class ChaptersFilter : IFilter
	{
		public const string CONTENT_RATING_SAFE = "safe";
		public const string CONTENT_RATING_SUGGESTIVE = "suggestive";
		public const string CONTENT_RATING_EROTICA = "erotica";
		public const string CONTENT_RATING_PORNOGRAPHIC = "pornographic";

		public static string[] ContentRatingsAll => new[] 
		{ 
			CONTENT_RATING_SAFE, 
			CONTENT_RATING_EROTICA, 
			CONTENT_RATING_PORNOGRAPHIC, 
			CONTENT_RATING_SUGGESTIVE 
		};

		public int Limit { get; set; } = 500;

		public int Offset { get; set; } = 0;

		public string[] TranslatedLanguage { get; set; } = Array.Empty<string>();

		public string[] OriginalLanguage { get; set; } = Array.Empty<string>();

		public string[] ExcludedOriginalLanguage { get; set; } = Array.Empty<string>();

		public string[] ContentRating { get; set; } = Array.Empty<string>();

		public string[] ExcludedGroups { get; set; } = Array.Empty<string>();

		public string[] ExcludedUploaders { get; set; } = Array.Empty<string>();

		public bool? IncludeFutureUpdates { get; set; }

		public DateTime? CreatedAtSince { get; set; }

		public DateTime? UpdatedAtSince { get; set; }

		public DateTime? PublishAtSince { get; set; }

		public Dictionary<OrderKey, OrderValue> Order { get; set; } = new();

		public string[] Includes { get; set; } = Array.Empty<string>();

		public bool? IncludeEmptyPages { get; set; }

		public bool? IncludeFuturePublishAt { get; set; }

		public bool? IncludeExternalUrl { get; set; }

		public string BuildQuery()
		{
			return new FilterBuilder()
				.Add("limit", Limit)
				.Add("offset", Offset)
				.Add("translatedLanguage", TranslatedLanguage)
				.Add("originalLanguage", OriginalLanguage)
				.Add("excludedOriginalLanguage", ExcludedOriginalLanguage)
				.Add("contentRating", ContentRating)
				.Add("excludedGroups", ExcludedGroups)
				.Add("excludedUploaders", ExcludedUploaders)
				.Add("includeFutureUpdates", IncludeFutureUpdates)
				.Add("createdAtSince", CreatedAtSince)
				.Add("updatedAtSince", UpdatedAtSince)
				.Add("publishAtSince", PublishAtSince)
				.Add("order", Order)
				.Add("includes", Includes)
				.Add("includeEmptyPages", IncludeEmptyPages)
				.Add("includeFuturePublishAt", IncludeFuturePublishAt)
				.Add("includeExternalUrl", IncludeExternalUrl)
				.Build();
		}

		public enum OrderKey
		{
			createdAt,
			updatedAt,
			publishAt,
			readableAt,
			volume,
			chapter
		}

		public enum OrderValue
		{
			asc,
			desc
		}
	}
}
