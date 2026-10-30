using System.Text.Json;

namespace CardboardBox.Anime.Patreon;

internal sealed class PatreonScrapeState
{
	private const string FileName = ".patreon-state.json";
	private readonly HashSet<string> _postIds;

	private PatreonScrapeState(string path, IEnumerable<string>? postIds = null)
	{
		Path = path;
		_postIds = new HashSet<string>(postIds ?? [], StringComparer.Ordinal);
	}

	public string Path { get; }

	public bool Contains(string postId) => _postIds.Contains(postId);

	public bool Add(string postId) => _postIds.Add(postId);

	public long? PostIdWatermark => _postIds
		.Select(postId => long.TryParse(postId, out var value) ? value : (long?)null)
		.Where(value => value is not null)
		.Max();

	public static async Task<PatreonScrapeState> LoadAsync(string directory, CancellationToken token)
	{
		var path = System.IO.Path.Combine(directory, FileName);
		if (!File.Exists(path))
			return new PatreonScrapeState(path);

		await using var stream = File.OpenRead(path);
		var document = await JsonSerializer.DeserializeAsync<StateDocument>(stream, cancellationToken: token) ??
			throw new InvalidDataException($"The Patreon scrape state is empty: {path}");
		return new PatreonScrapeState(path, document.PostIds);
	}

	public async Task SaveAsync(CancellationToken token)
	{
		var document = new StateDocument(_postIds.Order(StringComparer.Ordinal).ToArray());
		await using var stream = File.Create(Path);
		await JsonSerializer.SerializeAsync(stream, document, new JsonSerializerOptions
		{
			WriteIndented = true,
		}, token);
	}

	private sealed record StateDocument(string[] PostIds);
}
