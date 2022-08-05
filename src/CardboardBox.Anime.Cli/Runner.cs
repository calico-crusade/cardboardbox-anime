using CardboardBox.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using SLImage = SixLabors.ImageSharp.Image;

namespace CardboardBox.Anime.Cli
{
	using Core;
	using Core.Models;
	using Funimation;
	using Vrv;

	public interface IRunner
	{
		Task<int> Run(string[] args);
	}

	public class Runner : IRunner
	{
		private const string VRV_JSON = "vrv2.json";
		private const string FUN_JSON = "fun.json";
		private const string VRV_FORMAT_JSON = "vrv-formatted.json";

		private readonly IVrvApiService _vrv;
		private readonly IFunimationApiService _fun;
		private readonly ILogger _logger;
		private readonly IApiService _api;
		private readonly IAnimeMongoService _mongo;

		public Runner(
			IVrvApiService vrv, 
			ILogger<Runner> logger,
			IFunimationApiService fun,
			IApiService api,
			IAnimeMongoService mongo)
		{
			_vrv = vrv;
			_logger = logger;
			_fun = fun;
			_api = api;
			_mongo = mongo;
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
					case "fun": await FetchFunimationResources(); break;
					case "sizes": await DeteremineImageSizes(); break;
					case "all": await All(); break;
					case "reformat": await ReformatIds(); break;
					case "load": await Load(); break;
					case "test": await Test(); break;
					case "clean": await Clean(); break;
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

		public async Task Test()
		{
			await _mongo.RegisterIndexes();
		}

		public async Task Load()
		{
			using var io = File.OpenRead("all.json");
			var data = await JsonSerializer.DeserializeAsync<Anime[]>(io);

			_logger.LogInformation("File Loaded");

			if (data == null)
			{
				_logger.LogError("Data is null");
				return;
			}

			foreach (var item in data)
				item.Id = null;

			_logger.LogInformation("Ids nulled");

			await _mongo.Upsert(data);

			_logger.LogInformation("Data loaded");
		}

		public async Task FetchFunimationResources()
		{
			var data = await _fun.All().ToListAsync();
			using var io = File.OpenWrite(FUN_JSON);
			await JsonSerializer.SerializeAsync(io, data);
		}

		public async Task DeteremineImageSizes()
		{
			var dic = new Dictionary<string, List<(int width, int height, string source)>>();
			var data = await _fun.All().ToListAsync();

			for(var i = 0; i < data.Count && i < 5; i++)
			{
				var cur = data[i];
				foreach (var im in cur.Images)
				{
					if (string.IsNullOrEmpty(im.Source))
					{
						_logger.LogInformation($"Skipping \"{im.PlatformId}\" for \"{cur.Title}\" as it's source is empty");
						continue;
					}

					using var res = await _api.Create(im.Source).Result();
					if (res == null || !res.IsSuccessStatusCode)
					{
						_logger.LogError("Failed to fetch resource: " + im.Source);
						continue;
					}

					using var io = await res.Content.ReadAsStreamAsync();
					using var image = await SLImage.LoadAsync(io);

					if (!dic.ContainsKey(im.PlatformId)) dic.Add(im.PlatformId, new());

					dic[im.PlatformId].Add((image.Width, image.Height, im.Source));
				}
			}

			_logger.LogInformation("Results:");
			foreach(var (type, sizes) in dic)
			{
				_logger.LogInformation(type);
				foreach (var (w, h, _) in sizes)
					_logger.LogInformation($"\t{w} x {h}");
			}
		}

		public async Task All()
		{
			var services = new IAnimeApiService[] { _vrv, _fun };

			var tasks = services.Select(t => t.All().ToListAsync().AsTask());
			var data = (await Task.WhenAll(tasks)).SelectMany(t => t).ToArray();
			if (data == null)
			{
				_logger.LogError("Data returned is null");
				return;
			}

			using var io = File.OpenWrite("all.json");
			await JsonSerializer.SerializeAsync(io, data);
		}

		public async Task FormatVrvResources()
		{
			var data = await _vrv.All().ToListAsync();
			using var io = File.OpenWrite(VRV_JSON);
			await JsonSerializer.SerializeAsync(io, data);
		}

		public async Task ReformatIds()
		{
			const string PATH = "all.json";
			using var i = File.OpenRead(PATH);
			var data = await JsonSerializer.DeserializeAsync<Anime[]>(i);
			await i.DisposeAsync();

			if (data == null)
			{
				_logger.LogError("Data is null");
				return;
			}

			foreach (var anime in data)
				anime.HashId = $"{anime.PlatformId}-{anime.AnimeId}-{anime.Title}".MD5Hash();

			File.Delete(PATH);

			using var o = File.OpenWrite(PATH);
			await JsonSerializer.SerializeAsync(o, data);
		}

		public async Task FetchVrvResources()
		{
			var output = new List<VrvResourceResult>();

			var ops = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
			foreach (var op in ops)
			{
				var resources = await _vrv.Fetch(op.ToString());
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

		public async Task Clean()
		{
			var data = await _mongo.All(1, 9000);
			if (data == null)
			{
				_logger.LogError("Data is null");
				return;
			}

			var all = data.Results.ToArray();

			var delParan = (string item) =>
			{
				if (!item.Contains("(")) return item.ToLower().Trim();
				return item.Split('(').First().Trim().ToLower();
			};

			foreach(var anime in all)
			{
				anime.Metadata.Languages = anime.Metadata.Languages.Select(delParan).Distinct().ToList();
				anime.Metadata.Ratings = anime.Metadata.Ratings.Select(t => t.ToLower().Trim().Split('|')).SelectMany(t => t).Distinct().ToList();
				anime.Metadata.Tags = anime.Metadata.Tags.Select(t => t.ToLower().Trim()).Distinct().ToList();
			}

			await _mongo.Upsert(all);
		}
	}
}
