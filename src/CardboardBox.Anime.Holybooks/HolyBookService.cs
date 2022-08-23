using CardboardBox.Http;
using Microsoft.Extensions.Configuration;
using Octokit;
using System.Web;

namespace CardboardBox.Anime.Holybooks
{
	public interface IHolyBooksService
	{
		Task<Language[]?> GetLanguages();
		Task<RepoFile[]?> GetFiles(string language);
	}

	public class HolyBooksService : IHolyBooksService
	{
		private readonly IConfiguration _config;
		private readonly CacheItem<Language[]> _languages;
		private readonly Dictionary<string, CacheItem<RepoFile[]>> _fileCache;
		private readonly GitHubClient _client;

		public string Repo => _config["Github:Repo"];
		public string Owner => _config["Github:Owner"];

		public HolyBooksService(
			IConfiguration config)
		{
			_config = config;

			var product = new ProductHeaderValue(_config["Github:Product"]);
			_client = new GitHubClient(product);

			_languages = new CacheItem<Language[]>(GetRawLanguages);
			_fileCache = new Dictionary<string, CacheItem<RepoFile[]>>();
		}

		public Task<Language[]?> GetLanguages() => _languages.Get();

		public Task<RepoFile[]?> GetFiles(string file)
		{
			if (!_fileCache.ContainsKey(file))
				_fileCache.Add(file, new CacheItem<RepoFile[]>(() => GetRawFiles(file)));

			return _fileCache[file]?.Get() ?? Task.FromResult<RepoFile[]?>(null);
		}

		public async Task<Language[]> GetRawLanguages()
		{
			var contents = await _client.Repository.Content.GetAllContents(Owner, Repo);

			return contents
				.Where(t => t.Type == ContentType.Dir)
				.Select(t => new Language(t.Name, t.Path))
				.ToArray();
		}

		public async Task<RepoFile[]> GetRawFiles(string language)
		{
			var encoded = HttpUtility.UrlEncode(language);
			var contents = await _client.Repository.Content.GetAllContents(Owner, Repo, encoded);

			return contents
				.Where(t => t.Type == ContentType.File)
				.Select(t => new RepoFile(t.Name, t.Path, t.DownloadUrl))
				.ToArray();
		}
	}
}
