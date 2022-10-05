namespace CardboardBox.Epub
{
	public interface IEpubBuilderStylesheets
	{
		Task AddStylesheetFromFile(string name, string path, bool global = true);
		Task AddStylesheet(string name, string content, bool global = true);
		Task AddStylesheet(string name, Stream stream, bool global = true);
		Task AddStylesheet(string name, byte[] content, bool global = true);
	}

	public partial class EpubBuilder
	{
		public async Task AddStylesheetFromFile(string name, string path, bool global = true)
		{
			using var io = File.OpenRead(path);
			await AddStylesheet(name, io);
		}

		public async Task AddStylesheet(string name, string content, bool global = true)
		{
			using var io = content.ToStream();
			await AddStylesheet(name, io, global);
		}

		public async Task AddStylesheet(string name, byte[] content, bool global = true)
		{
			using var io = content.ToStream();
			await AddStylesheet(name, io, global);
		}

		public async Task AddStylesheet(string name, Stream stream, bool global = true)
		{
			var relPath = await AddFile(name, stream, FileType.Stylesheet);
			if (global) GlobalStylesheets.Add(relPath);
		}
	}
}
