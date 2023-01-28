using System.IO.Compression;

namespace CardboardBox.Epub.Management;

public class FileSystemService : IManagementSystem
{
	public string WorkingDirectory { get; }
	public string OutputFile { get; }

	public FileSystemService(string outputFile, string? workingDirectory = null)
	{
		WorkingDirectory = workingDirectory ?? Path.Combine(Path.GetTempPath(), $"cba-epub-{Guid.NewGuid()}"); ;
		OutputFile = outputFile;
	}

	public void Initialize()
	{
		if (!Directory.Exists(WorkingDirectory))
			Directory.CreateDirectory(WorkingDirectory);
	}

	public Task Finish()
	{
		if (File.Exists(OutputFile))
			File.Delete(OutputFile);

		ZipFile.CreateFromDirectory(WorkingDirectory, OutputFile, CompressionLevel.Fastest, false);

		new DirectoryInfo(WorkingDirectory).Delete(true);

		return Task.CompletedTask;
	}

	public async Task Add(string filename, Stream content)
	{
		var path = Path.Combine(WorkingDirectory, filename);

		var dir = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		using var io = File.Create(path);
		await content.CopyToAsync(io);
	}
}
