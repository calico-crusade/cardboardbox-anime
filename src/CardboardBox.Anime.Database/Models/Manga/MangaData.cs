namespace CardboardBox.Anime.Database;

public class MangaData : MangaWithChapters
{
	[JsonIgnore]
	public override DbMangaChapter[] Chapters { get; set; } = Array.Empty<DbMangaChapter>();

	[JsonPropertyName("chapter")]
	public DbMangaChapter Chapter { get; set; } = new();

	[JsonPropertyName("volumes")]
	public Volume[] Volumes { get; set; } = Array.Empty<Volume>();

	[JsonPropertyName("progress")]
	public DbMangaProgress? Progress { get; set; }

	[JsonPropertyName("stats")]
	public MangaStats? Stats { get; set; }

	[JsonPropertyName("volumeIndex")]
	public int VolumeIndex { get; set; }
}

public class Volume
{
	[JsonPropertyName("name")]
	public double? Name { get; set; }

	[JsonPropertyName("collapse")]
	public bool Collapse { get; set; } = false;

	[JsonPropertyName("read")]
	public bool Read { get; set; } = false;

	[JsonPropertyName("inProgress")]
	public bool InProgress { get; set; } = false;

	[JsonPropertyName("chapters")]
	public List<VolumeChapter> Chapters { get; set; } = new();
}

public class VolumeChapter
{
	[JsonPropertyName("read")]
	public bool Read { get; set; } = false;

	[JsonPropertyName("readIndex")]
	public int? ReadIndex { get; set; }

	[JsonPropertyName("pageIndex")]
	public int? PageIndex { get; set; }

	[JsonPropertyName("versions")]
	public DbMangaChapter[] Versions { get; set; } = Array.Empty<DbMangaChapter>();

	[JsonPropertyName("open")]
	public bool Open { get; set; } = false;

	[JsonPropertyName("progress")]
	public double? Progress { get; set; } = null;
}
