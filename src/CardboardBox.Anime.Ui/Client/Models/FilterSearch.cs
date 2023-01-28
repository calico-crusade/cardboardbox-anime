namespace CardboardBox.Anime.Ui.Client.Models;

public class FilterSearch
{
	[JsonPropertyName("page")]
	public int Page { get; set; } = 1;

	[JsonPropertyName("size")]
	public int Size { get; set; } = 50;

	[JsonPropertyName("search")]
	public string Search { get; set; } = string.Empty;

	[JsonPropertyName("queryables")]
	public Queryable Queryables { get; set; } = new();

	[JsonPropertyName("asc")]
	public bool Ascending { get; set; } = true;

	[JsonPropertyName("mature")]
	public MatureType Mature { get; set; } = MatureType.Both;

	public enum MatureType : int
	{
		Both = 0,
		Mature = 1,
		Everyone = 2
	}

	public class Queryable
	{
		[JsonPropertyName("languages")]
		public string[] Languages { get; set; } = Array.Empty<string>();

		[JsonPropertyName("types")]
		public string[] Types { get; set; } = Array.Empty<string>();

		[JsonPropertyName("platforms")]
		public string[] Platforms { get; set; } = Array.Empty<string>();

		[JsonPropertyName("tags")]
		public string[] Tags { get; set; } = Array.Empty<string>();

		[JsonPropertyName("video types")]
		public string[] VideoTypes { get; set; } = Array.Empty<string>();
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

public class ListFilterSearch : FilterSearch
{
	[JsonPropertyName("listId")]
	public long? ListId { get; set; }
}
