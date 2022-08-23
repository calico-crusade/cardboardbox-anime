namespace CardboardBox.Anime.Holybooks
{
	public class Language
	{
		public string? Path { get; set; }
		public string? Name { get; set; }

		public Language() { }

		public Language(string name, string path)
		{
			Name = name;
			Path = path;
		}
	}

	public class RepoFile : Language
	{
		public string? DownloadUrl { get; set; }

		public RepoFile(string name, string path, string download) : base(name, path)
		{
			DownloadUrl = download;
		}
	}
}