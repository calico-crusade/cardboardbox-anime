namespace CardboardBox.Anime.Crunchyroll;

public interface ICrunchyrollConfig
{
	string ResourceList { get; set; }
	Dictionary<string, string> Query { get; set; }
}

public class CrunchyrollConfig : ICrunchyrollConfig
{
	public string ResourceList { get; set; } = string.Empty;

	public Dictionary<string, string> Query { get; set; } = new();
}
