namespace CardboardBox.Anime.Database;

using Core.Models;

public class DbImage
{
	[JsonPropertyName("width")]
	public int? Width { get; set; }

	[JsonPropertyName("height")]
	public int? Height { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; } = "";

	[JsonPropertyName("source")]
	public string Source { get; set; } = "";

	[JsonPropertyName("platformId")]
	public string PlatformId { get; set; } = "";

	public static implicit operator DbImage(Image i)
	{
		return new DbImage
		{
			Width = i.Width,
			Height = i.Height,
			PlatformId = i.PlatformId,
			Source = i.Source,
			Type = i.Type,
		};
	}
}
