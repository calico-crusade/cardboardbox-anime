namespace CardboardBox.Anime.Bot.Services;

public interface IPersistenceService
{
	Task<IPersistence> Load();
}

public class PersistenceService : IPersistenceService
{
	private const string DIR = "persist";
	private const string FILE_PATH = "persistence.json";

	public async Task<IPersistence> Load()
	{
		var path = GetPath();

		if (!File.Exists(path))
		{
			var res = new Persistence();
			res.Saver = () => Save(res);
			return res;
		}

		using var io = File.OpenRead(path);
		var results = await JsonSerializer.DeserializeAsync<Persistence>(io) ?? new();
		results.Saver = () => Save(results);
		return results;
	}

	public async Task Save(IPersistence data)
	{
		using var io = File.Create(GetPath());
		await JsonSerializer.SerializeAsync(io, data);
	}

	public string GetPath()
	{
		if (!Directory.Exists(DIR))
			Directory.CreateDirectory(DIR);

		return Path.Combine(DIR, FILE_PATH);
	}
}

public interface IPersistence
{
	DateTime? LastCheck { get; set; }

	Task Save();
}

public class Persistence : IPersistence
{
	public DateTime? LastCheck { get; set; }

	public Func<Task>? Saver { get; set; }

	public Task Save()
	{
		return Saver == null ? Task.CompletedTask : Saver();
	}
}
