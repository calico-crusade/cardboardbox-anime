namespace CardboardBox.Anime.Database;

public class MangaData : MangaWithChapters
{
	[JsonPropertyName("chapter")]
	public DbMangaChapter Chapter { get; set; } = new();

	[JsonPropertyName("volumes")]
	public Volume[] Volumes { get; set; } = Array.Empty<Volume>();

	[JsonPropertyName("progress")]
	public DbMangaProgress? Progress { get; set; }

	[JsonPropertyName("stats")]
	public MangaStats? Stats { get; set; }
}

public class Volume
{
	[JsonPropertyName("name")]
	public double? Name { get; set; }

	[JsonPropertyName("collapse")]
	public bool Collapse { get; set; } = false;

	[JsonPropertyName("chapters")]
	public List<VolumeChapter> Chapters { get; set; } = new();
}

public class VolumeChapter : DbMangaChapter
{
	[JsonPropertyName("read")]
	public bool Read { get; set; } = false;

	[JsonPropertyName("versions")]
	public List<DbMangaChapter> Versions { get; set; } = new();

	[JsonPropertyName("open")]
	public bool Open { get; set; } = false;

	[JsonPropertyName("progress")]
	public double? Progress { get; set; } = null;
}
