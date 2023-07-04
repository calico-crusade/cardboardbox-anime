namespace CardboardBox.Epub;

using Management;
using Metadata;

public interface IEpubBuilder : IEpubBuilderStylesheets, IEpubBuilderImage, IEpubBuilderCover, IEpubBuilderMetadata, IEpubBuilderChapters
{
	Task<string> AddFile(string name, Stream stream, FileType type);
}

public interface IEpub : IAsyncDisposable
{
	Task<IEpubBuilder> Start();
}

public partial class EpubBuilder : IEpub, IEpubBuilder
{
	public const string EPUB_TYPE_CHAPTER = "bodymatter chapter";
	public const string EPUB_TYPE_COVER = "cover";
	public const string HTML_BODY_CLASS_NOMARGIN = "nomargin center";

	private readonly IManagementSystem _files;

	public string ContentDirectory { get; set; } = "OEPBS";
	public string MetaInfoDirectory { get; set; } = "META-INF";

	public string StylesDirectory { get; set; } = "Styles";
	public string ImagesDirectory { get; set; } = "Images";
	public string TextDirectory { get; set; } = "Text";

	public ContentOpf Content { get; set; }
	public Ncx Ncx { get; set; }

	public List<string> GlobalStylesheets { get; set; } = new();

	private EpubBuilder(IManagementSystem file, string title, string? id)
	{
		id ??= Guid.NewGuid().ToString();
		_files = file;
		Content = new ContentOpf(new MetaData(title, id));
		Ncx = new Ncx(title, id);
	}

	public async Task<IEpubBuilder> Start()
	{
		_files.Initialize();
		//Ensure the Table of Contents gets added towards the beginning of the document
		Content.SpineReferences.Add("toc.xhtml");
		//Ensure mimetype is first file in zip
		await AddEntry("mimetype", GenerateMimetype());
		return this;
	}

	public async ValueTask DisposeAsync()
	{
		//Add Table of Contents
		await AddFile("toc.xhtml", GenerateToc().ToStream(), FileType.Nav);

		await AddEntry(Path.Combine(MetaInfoDirectory, "container.xml"), GenerateContainer());
		await AddEntry(Path.Combine(ContentDirectory, "content.opf"), GenerateContentOpf());
		await AddEntry(Path.Combine(ContentDirectory, "toc.ncx"), GenerateNcx());

		await _files.Finish();
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
		await _files.Add(filename, content);
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

	public static IEpub Create(string title, string output, string? id = null, string? workingDir = null)
	{
		var io = new FileSystemService(output, workingDir);
		return new EpubBuilder(io, title, id);
	}

	public static IEpub Create(string title, Stream target, string? id = null)
	{
		var file = new MemorySystemService(target);
		return new EpubBuilder(file, title, id);
	}
}