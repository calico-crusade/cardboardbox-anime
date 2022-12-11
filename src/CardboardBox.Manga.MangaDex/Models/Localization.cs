namespace CardboardBox.Manga.MangaDex.Models
{
	[JsonConverter(typeof(MangaDexDictionaryParser))]
	public class Localization : Dictionary<string, string> { }
}
