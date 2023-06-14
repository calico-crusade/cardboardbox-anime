namespace CardboardBox.LightNovel.Core;

public class SourceVolume
{
	public string Title { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
	public SourceChapterItem[] Chapters { get; set; } = Array.Empty<SourceChapterItem>();
	public string[] Inserts { get; set; } = Array.Empty<string>();
	public string[] Forwards { get; set; } = Array.Empty<string>();
}

public class SourceChapterItem
{
	public string Title { get; set; } = string.Empty;
	public string Url { get; set; } = string.Empty;
}
