using CardboardBox.Extensions;

namespace CardboardBox.Anime.Core;

public interface IFileCacheService
{
	Task<StreamResult> GetFile(string url);
}

public class FileCacheService : IFileCacheService
{
	private const string FILE_CACHE_DIR = "ProxyCache";
	private readonly IApiService _api;
	private readonly string _cacheDirectory;

	public FileCacheService(IApiService api, string cacheDirectory = FILE_CACHE_DIR)
	{
		_api = api;
		_cacheDirectory = cacheDirectory;
	}

	public async Task<StreamResult> GetFile(string url)
	{
		// Older LNT chapters contain WordPress image-proxy URLs. Fetch the
		// original upload instead, using its own cache key as well.
		url = NormalizeUrl(url);
		if (!Directory.Exists(_cacheDirectory))
			Directory.CreateDirectory(_cacheDirectory);

		var hash = url.MD5Hash();

		var cacheInfo = await ReadCacheInfo(hash);
		if (cacheInfo != null)
			return new (ReadFile(hash), cacheInfo.Name, cacheInfo.MimeType);

		var io = new MemoryStream();
		var (stream, _, file, type) = await _api.GetData(url);
		using (stream)
			await stream.CopyToAsync(io);
		io.Position = 0;
		file = file.Replace("\"", "").PurgePathChars();
		cacheInfo = new CacheItem(file, type, DateTime.Now);
		var worked = await WriteFile(io, hash);
		if (worked)
			await WriteCacheInfo(hash, cacheInfo);
		io.Position = 0;

		return new (io, file, type);
	}

	public static string NormalizeUrl(string url)
	{
		if (!Uri.TryCreate(url, UriKind.Absolute, out var proxy) ||
			(proxy.Scheme != Uri.UriSchemeHttp && proxy.Scheme != Uri.UriSchemeHttps) ||
			proxy.Host is not ("i0.wp.com" or "i1.wp.com" or "i2.wp.com")) return url;

		const string hostPath = "/lightnovelstranslations.com";
		if (!proxy.AbsolutePath.StartsWith(hostPath + "/wp-content/uploads/", StringComparison.OrdinalIgnoreCase)) return url;

		// Resize/quality arguments belong to the proxy, not the source image.
		return "https://lightnovelstranslations.com" + proxy.AbsolutePath[hostPath.Length..];
	}

	public string FilePath(string hash) => Path.Combine(_cacheDirectory, $"{hash}.data");

	public string CachePath(string hash) => Path.Combine(_cacheDirectory, $"{hash}.cache.json");

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
