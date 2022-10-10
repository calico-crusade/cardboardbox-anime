namespace CardboardBox.LightNovel.Core
{
	public class SourceVolume
	{
		public string Title { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
		public SourceChapterItem[] Chapters { get; set; } = Array.Empty<SourceChapterItem>();

		public SourceVolume() { }

		public SourceVolume(string title, string url, SourceChapterItem[] chapters)
		{
			Title = title;
			Url = url;
			Chapters = chapters;
		}
	}

	public class SourceChapterItem
	{
		public string Title { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
	}
}
