namespace CardboardBox.Anime.Core.Models
{
	public class AnimeFilter : SearchFilter
	{
		[JsonPropertyName("queryables")]
		public Queryable Queryables { get; set; } = new();

		[JsonPropertyName("mature")]
		public MatureType Mature { get; set; } = MatureType.Both;

		[JsonPropertyName("listId")]
		public long? ListId { get; set; }

		public enum MatureType : int
		{
			Both = 0,
			Mature = 1,
			Everyone = 2
		}

		public class Queryable
		{
			[JsonPropertyName("languages")]
			public string[]? Languages { get; set; }

			[JsonPropertyName("types")]
			public string[]? Types { get; set; }

			[JsonPropertyName("platforms")]
			public string[]? Platforms { get; set; }

			[JsonPropertyName("tags")]
			public string[]? Tags { get; set; }

			[JsonPropertyName("video types")]
			public string[]? VideoTypes { get; set; }
		}

		public void Deconstruct(
			out int page, out int size, out string search, 
			out string[] langs, out string[] types, 
			out string[] plats, out string[] tags, 
			out string[] videoTypes,
			out bool asc, out MatureType mature)
		{
			page = Page;
			size = Size;
			search = Search ?? "";
			langs = Queryables.Languages ?? Array.Empty<string>();
			types = Queryables.Types ?? Array.Empty<string>();
			plats = Queryables.Platforms ?? Array.Empty<string>();
			tags = Queryables.Tags ?? Array.Empty<string>();
			videoTypes = Queryables.VideoTypes ?? Array.Empty<string>();
			asc = Ascending;
			mature = Mature;
		}
	}

	public class MangaFilter : SearchFilter
	{
		[JsonPropertyName("include")]
		public string[] Include { get; set; } = Array.Empty<string>();

		[JsonPropertyName("exclude")]
		public string[] Exclude { get; set; } = Array.Empty<string>();

		[JsonPropertyName("sort")]
		public int? Sort { get; set; }

		[JsonPropertyName("state")]
		public TouchedState State { get; set; } = TouchedState.All;

		public void Deconstruct(
			out int page, out int size, out string? search,
			out string[] exclude, out string[] include,
			out bool asc)
		{
			page = Page;
			size = Size;
			search = Search;
			exclude = Exclude;
			include = Include;
			asc = Ascending;
		}
	}

	public abstract class SearchFilter
	{
		[JsonPropertyName("page")]
		public int Page { get; set; } = 1;

		[JsonPropertyName("size")]
		public int Size { get; set; } = 100;

		[JsonPropertyName("search")]
		public string? Search { get; set; }

		[JsonPropertyName("asc")]
		public bool Ascending { get; set; } = true;
	}

	public enum TouchedState
	{
		All = 99,
		Favourite = 1,
		Completed = 2,
		InProgress = 3,
		Bookmarked = 4,
		Else = 5,
		Touched = 6
	}
}
