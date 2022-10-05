namespace CardboardBox.Epub
{
	public interface IChapterBuilder : IEpubBuilderImage
	{
		Task AddPageFromFile(string name, string path);
		Task AddPage(string name, string content);
		Task AddPage(string name, Stream stream);
		Task AddPage(string name, byte[] content);
		Task AddRawPage(string name, string content);
	}

	public class ChapterBuilder : IChapterBuilder
	{
		public bool First { get; set; } = true;

		public string Title { get; }

		public EpubBuilder Builder { get; }

		public ChapterBuilder(string title, EpubBuilder builder)
		{
			Title = title;
			Builder = builder;
		}

		public string GenerateHtml(string content)
		{
			return Builder.GenerateHtmlPage(content);
		}

		public async Task AddPageFromFile(string name, string path)
		{
			using var io = File.OpenRead(path);
			await AddPage(name, io);
		}

		public async Task AddPage(string name, string content)
		{
			using var io = content.ToStream();
			await AddPage(name, io);
		}

		public async Task AddPage(string name, byte[] content)
		{
			using var io = content.ToStream();
			await AddPage(name, io);
		}

		public async Task AddPage(string name, Stream stream)
		{
			//Add to the manifest & add the actual file to the zip
			await Builder.AddFile(name, stream, EpubBuilder.FileType.Page);

			//Add to the spine
			Builder.Content.SpineReferences.Add(name);

			//Add to NCX (if it's the first page in the chapter)
			if (!First) return;

			First = false;
			Builder.Ncx.Nav.Add((Title, Path.Combine(Builder.TextDirectory, name)));
		}

		public async Task AddRawPage(string name, string content)
		{
			var html = GenerateHtml(content);
			await AddPage(name, html);
		}

		public async Task AddImage(string name, string path)
		{
			using var io = File.OpenRead(path);
			await AddImage(name, io);
		}

		public async Task AddImage(string name, byte[] content)
		{
			using var io = content.ToStream();
			await AddImage(name, io);
		}

		public async Task AddImage(string name, Stream stream)
		{
			name = name.FixFileName();
			var relPath = await Builder.AddFile(name, stream, EpubBuilder.FileType.Image);

			var html = GenerateInsert(relPath, name.Replace(".", ""));
			var nwp = Path.GetFileNameWithoutExtension(name) + ".xhtml";

			using var htmlIO = html.ToStream();
			await AddPage(nwp, htmlIO);
		}

		public string GenerateInsert(string path, string name)
		{
			return Builder.GenerateHtmlPage($"<img alt=\"{name}\" class=\"insert\" src=\"{path}\"/>", true);
		}
	}
}
