namespace CardboardBox.Manga.MangaDex.Models
{
	[JsonConverter(typeof(MangaDexParser<IRelationship>))]
	public interface IRelationship : IJsonType
	{
		string Id { get; set; }
	}

	public class PersonRelationship : MangaDexModel<PersonRelationship.AttributesModel>, IRelationship
	{
		public class AttributesModel
		{
			[JsonPropertyName("name")]
			public string Name { get; set; } = string.Empty;

			[JsonPropertyName("imageUrl")]
			public string? ImageUrl { get; set; }

			[JsonPropertyName("twitter")]
			public string? Twitter { get; set; }

			[JsonPropertyName("pixiv")]
			public string? Pixiv { get; set; }

			[JsonPropertyName("melonBook")]
			public string? MelonBook { get; set; }

			[JsonPropertyName("fanBox")]
			public string? FanBox { get; set; }

			[JsonPropertyName("booth")]
			public string? Booth { get; set; }

			[JsonPropertyName("nicoVideo")]
			public string? NicoVideo { get; set; }

			[JsonPropertyName("skeb")]
			public string? Skeb { get; set; }

			[JsonPropertyName("fantia")]
			public string? Fantia { get; set; }

			[JsonPropertyName("tumblr")]
			public string? Tumblr { get; set; }

			[JsonPropertyName("youtube")]
			public string? Youtube { get; set; }

			[JsonPropertyName("weibo")]
			public string? Weibo { get; set; }

			[JsonPropertyName("naver")]
			public string? Naver { get; set; }

			[JsonPropertyName("website")]
			public string? Website { get; set; }

			[JsonPropertyName("createdAt")]
			public DateTime CreatedAt { get; set; }

			[JsonPropertyName("updatedAt")]
			public DateTime UpdatedAt { get; set; }

			[JsonPropertyName("version")]
			public int Version { get; set; }
		}
	}

	public class CoverArtRelationship : MangaDexModel<CoverArtRelationship.AttributesModel>, IRelationship
	{
		public class AttributesModel
		{
			[JsonPropertyName("description")]
			public string Description { get; set; } = string.Empty;

			[JsonPropertyName("volume")]
			public string Volume { get; set; } = string.Empty;

			[JsonPropertyName("fileName")]
			public string FileName { get; set; } = string.Empty;

			[JsonPropertyName("locale")]
			public string Locale { get; set; } = string.Empty;

			[JsonPropertyName("createdAt")]
			public DateTime CreatedAt { get; set; }

			[JsonPropertyName("updatedAt")]
			public DateTime UpdatedAt { get; set; }

			[JsonPropertyName("version")]
			public int Version { get; set; }
		}
	}

	public class RelatedDataRelationship : MangaDexManga, IRelationship
	{
		[JsonPropertyName("related")]
		public string Related { get; set; } = string.Empty;

	}

	public class ScanlationGroupRelationship : MangaDexModel<ScanlationGroupRelationship.AttributesModel>, IRelationship
	{
		public class AttributesModel
		{
			[JsonPropertyName("name")]
			public string Name { get; set; } = string.Empty;

			[JsonPropertyName("locked")]
			public bool Locked { get; set; }

			[JsonPropertyName("website")]
			public string? Website { get; set; }

			[JsonPropertyName("ircServer")]
			public string? IrcServer { get; set; }

			[JsonPropertyName("ircChannel")]
			public string? IrcChannel { get; set; }

			[JsonPropertyName("discord")]
			public string? Discord { get; set; }

			[JsonPropertyName("contactEmail")]
			public string? ContactEmail { get; set; }

			[JsonPropertyName("Description")]
			public string? Description { get; set; }

			[JsonPropertyName("twitter")]
			public string? Twitter { get; set; }

			[JsonPropertyName("mangaUpdates")]
			public string? MangaUpdates { get; set; }

			[JsonPropertyName("focusedLanguages")]
			public string[] FocusedLanguages { get; set; } = Array.Empty<string>();

			[JsonPropertyName("official")]
			public bool Official { get; set; }

			[JsonPropertyName("verified")]
			public bool Verified { get; set; }

			[JsonPropertyName("inactive")]
			public bool Inactive { get; set; }

			[JsonPropertyName("createdAt")]
			public DateTime CreatedAt { get; set; }

			[JsonPropertyName("updatedAt")]
			public DateTime UpdatedAt { get; set; }

			[JsonPropertyName("version")]
			public int Version { get; set; }
		}
	}

	public class UserRelationship : MangaDexModel<UserRelationship.AttributesModel>, IRelationship
	{
		public class AttributesModel
		{
			[JsonPropertyName("username")]
			public string Username { get; set; } = string.Empty;

			[JsonPropertyName("roles")]
			public string[] Roles { get; set; } = Array.Empty<string>();

			[JsonPropertyName("version")]
			public int Version { get; set; }
		}
	}
}
