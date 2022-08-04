using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CardboardBox.Anime.Cli
{
	using Vrv;

	public interface IRunner
	{
		Task<int> Run(string[] args);
	}

	public class Runner : IRunner
	{
		private const string VRV_JSON = "vrv.json";
		private const string VRV_FORMAT_JSON = "vrv-formatted.json";

		private readonly IVrvApiService _vrv;
		private readonly ILogger _logger;

		public Runner(
			IVrvApiService vrv, 
			ILogger<Runner> logger)
		{
			_vrv = vrv;
			_logger = logger;
		}

		public async Task<int> Run(string[] args)
		{
			try
			{
				_logger.LogInformation("Starting with args: " + string.Join(" ", args));
				var last = args.Last().ToLower();
				var command = string.IsNullOrEmpty(last) ? "fetch" : last;

				switch (command)
				{
					case "fetch": await FetchVrvResources(); break;
					case "format": await FormatVrvResources(); break;
					default: _logger.LogInformation("Invalid command: " + command); break;
				}

				_logger.LogInformation("Finished.");
				return 0;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing command: " + string.Join(" ", args));
				return 1;
			}
		}

		public async Task FormatVrvResources()
		{
			VrvResourceResult[]? data;
			using (var io = File.OpenRead(VRV_JSON))
				data = await JsonSerializer.DeserializeAsync<VrvResourceResult[]>(io);

			if (data == null)
			{
				_logger.LogError("Error occurred while reading JSON data");
				return;
			}

			var output = data.SelectMany(t => t.Items).Select(t => (VrvAnime)t).ToArray();
			using var io2 = File.OpenWrite(VRV_FORMAT_JSON);
			await JsonSerializer.SerializeAsync(io2, output);
		}

		public async Task FetchVrvResources()
		{
			var output = new List<VrvResourceResult>();

			var ops = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
			foreach (var op in ops)
			{
				var resources = await _vrv.FetchResources(op.ToString());
				if (resources == null)
				{
					_logger.LogWarning("Resource not found for: " + op);
					continue;
				}
				_logger.LogInformation($"{resources.Total} found for: {op}");
				output.Add(resources);
			}

			using var io = File.OpenWrite(VRV_JSON);
			await JsonSerializer.SerializeAsync(io, output);

			_logger.LogInformation("Finished writing");
		}
	}
}
