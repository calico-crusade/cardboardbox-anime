using System.IO.Compression;

namespace CardboardBox.Epub.Management;

public class MemorySystemService : IManagementSystem
{
	public Stream Output { get; }
	public ZipArchive Archive { get; }

	public MemorySystemService(Stream output)
	{
		Output = output;
		Archive = new ZipArchive(output, ZipArchiveMode.Create, true);
	}

	public void Initialize()
	{

	}

	public async Task Finish()
    {
        Archive.Dispose();
        await Output.FlushAsync();
	}

	public async Task Add(string filename, Stream content)
	{
		var entry = Archive.CreateEntry(filename);
		using var io = entry.Open();
		await content.CopyToAsync(io);
	}
}
