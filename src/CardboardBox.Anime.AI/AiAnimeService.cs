using Microsoft.Extensions.Configuration;

namespace CardboardBox.Anime.AI;

using Http;

public interface IAiAnimeService
{
	Task<AiResponse?> Text2Img(AiRequest request);
	Task<AiResponse?> Img2Img(AiRequestImg2Img request);
	Task<string[]> Embeddings();
	Task<LoraResponse[]?> Loras();
	Task<SamplerResponse[]?> Samplers();
	Task<string?> DownloadAndEncode(string url);
	Task<string> DecodeAndSave(string data, string dir);
	Task<string> DecodeAndSaveUrl(string data, string? dir = null);
}

public class AiAnimeService : IAiAnimeService
{
	private readonly IApiService _api;
	private readonly IConfiguration _config;

	public string AIUrl => _config["Ai:Url"] ?? throw new ArgumentNullException("Ai:Url");

	public AiAnimeService(
		IApiService api, 
		IConfiguration config)
	{
		_api = api;
		_config = config;
	}

	public Task<AiResponse?> Text2Img(AiRequest request)
	{
		return _api.Post<AiResponse, AiRequest>(AIUrl + "/sdapi/v1/txt2img", request);
	}

	public async Task<AiResponse?> Img2Img(AiRequestImg2Img request)
	{
		for(var i = 0; i < request.Images.Length; i++)
		{
			var image = request.Images[i];
			if (image.ToLower().StartsWith("http"))
				request.Images[i] = await DownloadAndEncode(image) ?? image;
		}

		return await _api.Post<AiResponse, AiRequestImg2Img>(AIUrl + "/sdapi/v1/img2img", request);
	}

	public async Task<string[]> Embeddings()
	{
		var embeds = await _api.Get<EmbeddingsResponse>(AIUrl + "/sdapi/v1/embeddings");
		return embeds?.Embeddings ?? Array.Empty<string>();
	}

	public Task<LoraResponse[]?> Loras()
	{
		return _api.Get<LoraResponse[]>(AIUrl + "/sdapi/v1/loras");
	}

	public Task<SamplerResponse[]?> Samplers()
	{
		return _api.Get<SamplerResponse[]>(AIUrl + "/sdapi/v1/samplers");
	}

	public async Task<string?> DownloadAndEncode(string url)
	{
		var (stream, _, file, type) = await _api.GetData(url);
        if (stream == null || !type.ToLower().StartsWith("image")) return null;

		var data = Array.Empty<byte>();
        using (var io = new MemoryStream())
        {
            await stream.CopyToAsync(io);
            io.Position = 0;
            data = io.ToArray();
            await stream.DisposeAsync();
        }

		return Convert.ToBase64String(data);
    }

	public async Task<string> DecodeAndSave(string data, string dir)
	{
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var path = Path.Combine(dir, Path.GetRandomFileName() + ".png");
        var bytes = Convert.FromBase64String(data);
        await File.WriteAllBytesAsync(path, bytes);
		return path;
    }

	public async Task<string> DecodeAndSaveUrl(string data, string? dir = null)
	{
		dir ??= Path.Combine("wwwroot", "image-cache");
		var path = await DecodeAndSave(data, dir);

		return "/" + string.Join("/", path.Split('/', '\\').Skip(1));
	}
}
