namespace CardboardBox.Epub
{
	using Metadata;

	public interface IEpubBuilder : IEpubBuilderStylesheets, IEpubBuilderImage, IEpubBuilderCover, IEpubBuilderMetadata, IEpubBuilderChapters
	{
		Task Finish();
	}

	public interface IEpub
	{
		IEpubBuilder Start(string outputPath);
		//IEpubBuilder Start(Stream output);
	}

	public partial class EpubBuilder : IEpub, IEpubBuilder
	{
		public const string EPUB_TYPE_CHAPTER = "bodymatter chapter";
		public const string EPUB_TYPE_COVER = "cover";
		public const string HTML_BODY_CLASS_NOMARGIN = "nomargin center";

		public string ContentDirectory { get; set; } = "OEPBS";
		public string MetaInfoDirectory { get; set; } = "META-INF";

		public string StylesDirectory { get; set; } = "Styles";
		public string ImagesDirectory { get; set; } = "Images";
		public string TextDirectory { get; set; } = "Text";

		public bool LeaveOpen { get; set; } = true;

		public string TempDirectory { get; set; }
		public string? OutputPath { get; set; }

		public ContentOpf Content { get; set; }
		public Ncx Ncx { get; set; }

		public List<string> GlobalStylesheets { get; set; } = new();

		private EpubBuilder(string title, string? id = null)
		{
			id ??= Guid.NewGuid().ToString();
			Content = new ContentOpf(new MetaData(title, id));
			Ncx = new Ncx(title, id);
			TempDirectory = Path.Combine(Path.GetTempPath(), "cba-epub-" + Guid.NewGuid().ToString());
		}

		public IEpubBuilder Start(string outputPath)
		{
			LeaveOpen = false;
			OutputPath = outputPath;

			if (!Directory.Exists(TempDirectory))
				Directory.CreateDirectory(TempDirectory);

			return this;
		}

		public async Task Finish()
		{
			//Add Table of Contents
			await AddFile("toc.xhtml", GenerateToc().ToStream(), FileType.Nav);
			Content.SpineReferences.Add("toc.xhtml");

			await AddEntry(Path.Combine(MetaInfoDirectory, "container.xml"), GenerateContainer());
			await AddEntry(Path.Combine(ContentDirectory, "content.opf"), GenerateContentOpf());
			await AddEntry(Path.Combine(ContentDirectory, "toc.ncx"), GenerateNcx());
			await AddEntry("mimetype", GenerateMimetype());

			var output = OutputPath ?? "output.epub";
			if (File.Exists(output))
				File.Delete(output);

			ZipFile.CreateFromDirectory(TempDirectory, output, CompressionLevel.Fastest, false);

			new DirectoryInfo(TempDirectory).Delete(true);
		}

		public async Task AddEntry(string filename, string content)
		{
			using var io = content.ToStream();
			await AddEntry(filename, io);
		}

		public async Task AddEntry(string filename, byte[] content)
		{
			using var io = content.ToStream();
			await AddEntry(filename, io);
		}

		public async Task AddEntry(string filename, Stream content)
		{
			var path = Path.Combine(TempDirectory, filename);

			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			using var io = File.Create(path);
			await content.CopyToAsync(io);
		}

		public async Task<string> AddFile(string name, Stream stream, FileType type)
		{
			var dir = type switch
			{
				FileType.Image => ImagesDirectory,
				FileType.Stylesheet => StylesDirectory,
				FileType.Cover => ImagesDirectory,
				FileType.Page => TextDirectory,
				FileType.Nav => TextDirectory,
				_ => throw new NotImplementedException()
			};

			var absPath = Path.Combine(ContentDirectory, dir, name);
			var path = Path.Combine(dir, name);
			var relPath = Path.Combine("..", path).Replace("\\", "/");

			await AddEntry(absPath, stream);

			switch(type)
			{
				case FileType.Image: Content.Manifest.AddImage(name, path); break;
				case FileType.Stylesheet: Content.Manifest.AddStylesheet(name, path); break;
				case FileType.Cover: Content.Manifest.AddImage(name, path, "cover-image"); break;
				case FileType.Page: Content.Manifest.AddPage(name, path); break;
				case FileType.Nav: Content.Manifest.AddPage(name, path, "nav"); break;
			}

			return relPath;
		}

		public enum FileType
		{
			Image = 1,
			Cover = 2,
			Stylesheet = 3,
			Page = 4,
			Nav = 5
		}

		public static IEpub Create(string title, string? id = null)
		{
			return new EpubBuilder(title, id);
		}
	}
}