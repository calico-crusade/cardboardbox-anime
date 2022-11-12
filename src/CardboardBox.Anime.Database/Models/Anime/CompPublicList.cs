namespace CardboardBox.Anime.Database
{
	public class CompPublicList
	{
		[JsonPropertyName("listId")]
		public long ListId { get; set; }

		[JsonPropertyName("listTitle")]
		public string ListTitle { get; set; } = "";

		[JsonPropertyName("listDescription")]
		public string ListDescription { get; set; } = "";

		[JsonPropertyName("listLastUpdate")]
		public DateTime ListLastUpdate { get; set; }

		[JsonPropertyName("listCount")]
		public long ListCount { get; set; }

		[JsonPropertyName("listTags")]
		public string[] ListTags { get; set; } = Array.Empty<string>();

		[JsonPropertyName("listLanguages")]
		public string[] ListLanguages { get; set; } = Array.Empty<string>();

		[JsonPropertyName("listLanguageTypes")]
		public string[] ListLanguageTypes { get; set; } = Array.Empty<string>();

		[JsonPropertyName("listVideoTypes")]
		public string[] ListVideoTypes { get; set; } = Array.Empty<string>();

		[JsonPropertyName("listPlatforms")]
		public string[] ListPlatforms { get; set; } = Array.Empty<string>();

		[JsonPropertyName("profileId")]
		public long ProfileId { get; set; }

		[JsonPropertyName("profileUsername")]
		public string ProfileUsername { get; set; } = "";

		[JsonPropertyName("profileAvatar")]
		public string ProfileAvatar { get; set; } = "";
	}
}
