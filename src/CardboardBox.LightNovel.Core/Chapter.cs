namespace CardboardBox.LightNovel.Core
{
	public class Chapter
	{
		public string BookTitle { get; set; } = string.Empty;
		public string ChapterTitle { get; set; } = string.Empty;
		public string Content { get; set; } = string.Empty;
		public string NextUrl { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;

		public Chapter() { }

		public Chapter(string bookTitle, string chapterTitle, string content, string nextUrl, string url)
		{
			BookTitle = bookTitle;
			ChapterTitle = chapterTitle;
			Content = content;
			NextUrl = nextUrl;
			Url = url;
		}
	}
}