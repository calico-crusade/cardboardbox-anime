using CardboardBox.Http;
using Flurl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardboardBox.Anime.Vrv
{
	public interface IVrvApiService
	{
		Task<VrvResourceResult?> FetchResources(string query, string sort = "alphabetical", int n = 100);
	}

	public class VrvApiService : IVrvApiService
	{
		private readonly IApiService _api;
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		private IVrvConfig _vrvConfig => _config.Bind<VrvConfig>("Vrv");

		public VrvApiService(
			IApiService api, 
			ILogger<VrvApiService> logger, 
			IConfiguration config)
		{
			_api = api;
			_logger = logger;
			_config = config;
		}

		public Task<VrvResourceResult?> FetchResources(string query, string sort = "alphabetical", int n = 100)
		{
			var url = _vrvConfig.ResourceList
				.SetQueryParam("q", query)
				.SetQueryParam("sort_by", sort)
				.SetQueryParam("n", n);

			foreach (var (name, res) in _vrvConfig.Query)
				url.SetQueryParam(name, res);

			return _api.Get<VrvResourceResult>(url.ToString());
		}
	}
}