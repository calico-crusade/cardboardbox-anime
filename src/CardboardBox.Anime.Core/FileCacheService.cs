namespace CardboardBox.Anime.Core;

public interface IFileCacheService
{
	Task<StreamResult> GetFile(string url);
}

public class FileCacheService : IFileCacheService
{
	private const string FILE_CACHE_DIR = "ProxyCache";
	private readonly IApiService _api;

	public FileCacheService(IApiService api)
	{
		_api = api;
	}

	public async Task<StreamResult> GetFile(string url)
	{
		if (!Directory.Exists(FILE_CACHE_DIR))
			Directory.CreateDirectory(FILE_CACHE_DIR);

		var hash = url.MD5Hash();

		var cacheInfo = await ReadCacheInfo(hash);
		if (cacheInfo != null)
			return new (ReadFile(hash), cacheInfo.Name, cacheInfo.MimeType);

		var io = new MemoryStream();
		var (stream, _, file, type) = await _api.GetData(url);
		await stream.CopyToAsync(io);
		io.Position = 0;
		cacheInfo = new CacheItem(file, type, DateTime.Now);
		var worked = await WriteFile(io, hash);
		if (worked)
			await WriteCacheInfo(hash, cacheInfo);
		io.Position = 0;

		return new (io, file, type);
	}

	public string FilePath(string hash) => Path.Combine(FILE_CACHE_DIR, $"{hash}.data");

	public string CachePath(string hash) => Path.Combine(FILE_CACHE_DIR, $"{hash}.cache.json");

	public Stream ReadFile(string hash)
	{
		var path = FilePath(hash);
		return File.OpenRead(path);
	}

	public async Task<bool> WriteFile(Stream stream, string hash)
	{
		try
		{
			var path = FilePath(hash);
			using var io = File.Create(path);
			await stream.CopyToAsync(io);
			return true;
		}
		catch 
		{
			return false;
		}
	}

	public async Task<CacheItem?> ReadCacheInfo(string hash)
	{
		var path = CachePath(hash);
		if (!File.Exists(path)) return null;

		using var io = File.OpenRead(path);
		return await JsonSerializer.DeserializeAsync<CacheItem>(io);
	}

	public async Task WriteCacheInfo(string hash, CacheItem item)
	{
		try
		{
			var path = CachePath(hash);
			using var io = File.Create(path);
			await JsonSerializer.SerializeAsync(io, item);
		}
		catch { }
	}

	public class CacheItem
	{
		public string Name { get; set; } = string.Empty;
		public string MimeType { get; set; } = string.Empty;
		public DateTime Created { get; set; }

		public CacheItem() { }

		public CacheItem(string name, string mimeType, DateTime created)
		{
			Name = name;
			MimeType = mimeType;
			Created = created;
		}

		public void Deconstruct(out string name, out string mimeType, out DateTime created)
		{
			name = Name;
			mimeType = MimeType;
			created = Created;
		}
	}
}

public record class StreamResult(Stream Stream, string Name, string Mimetype);