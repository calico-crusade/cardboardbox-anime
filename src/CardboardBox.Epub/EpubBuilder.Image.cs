namespace CardboardBox.Epub
{
	public interface IEpubBuilderImage
	{
		Task AddImage(string name, string path);
		Task AddImage(string name, Stream stream);
		Task AddImage(string name, byte[] content);
	}

	public interface IEpubBuilderCover
	{
		Task AddCoverImage(string name, string path);
		Task AddCoverImage(string name, Stream stream);
		Task AddCoverImage(string name, byte[] content);
	}

	public partial class EpubBuilder
	{
		public string GenerateCoverPage(string path)
		{
			return GenerateHtmlPage($"<img alt=\"Cover\" class=\"cover\" src=\"{path}\"/>", true, EPUB_TYPE_COVER);
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

		public  async Task AddImage(string name, Stream stream)
		{
			name = name.FixFileName();
			await AddFile(name, stream, FileType.Image);
		}

		public async Task AddCoverImage(string name, string path)
		{
			using var io = File.OpenRead(path);
			await AddCoverImage(name, io);
		}

		public async Task AddCoverImage(string name, byte[] content)
		{
			using var io = content.ToStream();
			await AddCoverImage(name, io);
		}

		public async Task AddCoverImage(string name, Stream stream)
		{
			name = name.FixFileName();
			const string COVER_HTML_PAGE = "cover.xhtml";

			if (Content.SpineReferences.Contains(COVER_HTML_PAGE))
				throw new Exception("EPUB already contains a cover!");

			//Add the actual image to the manifest and zip file
			var coverRelPath = await AddFile(name, stream, FileType.Cover);

			//Generate the actual cover page HTML
			var html = GenerateCoverPage(coverRelPath);
			var htmlPath = Path.Combine(TextDirectory, COVER_HTML_PAGE);
			
			//Add the cover path HTML to the manifest, zip file, ncx, and spine
			using var htmlIO = html.ToStream();
			await AddFile(COVER_HTML_PAGE, htmlIO, FileType.Page);
			Ncx.Nav.Insert(0, ("Cover", htmlPath)); //Ensure it's the first item in the list
			Content.SpineReferences.Insert(0, COVER_HTML_PAGE);
			Content.Meta.Cover = name;
		}
	}
}
