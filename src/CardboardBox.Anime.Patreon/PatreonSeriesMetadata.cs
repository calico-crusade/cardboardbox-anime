using System.Globalization;

namespace CardboardBox.Anime.Patreon;

public sealed class PatreonSeriesMetadata
{
	private readonly List<MetadataLine> _lines;

	private PatreonSeriesMetadata(string filePath, List<MetadataLine> lines)
	{
		FilePath = filePath;
		_lines = lines;
	}

	public string FilePath { get; }

	public string Directory => Path.GetDirectoryName(FilePath)!;

	public string? Collection => GetValue("Collection");

	public string? Posts => GetValue("Posts");

	public string? Author => GetValue("Author");

	public string? Home => GetValue("Home");

	public string? SourceUrl => Collection ?? Home ?? Posts;

	public DateTimeOffset? LastLoad
	{
		get
		{
			var value = GetValue("Last Load");
			if (string.IsNullOrWhiteSpace(value))
				return null;

			return DateTimeOffset.TryParse(
				value,
				CultureInfo.InvariantCulture,
				DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
				out var parsed)
				? parsed
				: null;
		}
	}

	public bool IsCollection => !string.IsNullOrWhiteSpace(Collection);

	public static async Task<PatreonSeriesMetadata> LoadAsync(string filePath, CancellationToken token = default)
	{
		var lines = await File.ReadAllLinesAsync(filePath, token);
		var parsed = lines.Select(ParseLine).ToList();
		return new PatreonSeriesMetadata(filePath, parsed);
	}

	public void SetLastLoad(DateTimeOffset value) => SetValue("Last Load", value.ToString("O", CultureInfo.InvariantCulture));

	public async Task SaveAsync(CancellationToken token = default)
	{
		var contents = string.Join(Environment.NewLine, _lines.Select(line => line.Render()));
		if (_lines.Count > 0)
			contents += Environment.NewLine;

		await File.WriteAllTextAsync(FilePath, contents, token);
	}

	private string? GetValue(string key) => _lines
		.FirstOrDefault(line => line.Key?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
		?.Value;

	private void SetValue(string key, string value)
	{
		var index = _lines.FindIndex(line => line.Key?.Equals(key, StringComparison.OrdinalIgnoreCase) == true);
		if (index >= 0)
		{
			_lines[index] = new MetadataLine(_lines[index].Key ?? key, value, null);
			return;
		}

		_lines.Add(new MetadataLine(key, value, null));
	}

	private static MetadataLine ParseLine(string line)
	{
		var separator = line.IndexOf(':');
		if (separator <= 0)
			return new MetadataLine(null, null, line);

		var key = line[..separator].Trim();
		var value = line[(separator + 1)..].Trim();
		return new MetadataLine(key, value, null);
	}

	private sealed record MetadataLine(string? Key, string? Value, string? Raw)
	{
		public string Render() => Raw ?? $"{Key}: {Value}";
	}
}
