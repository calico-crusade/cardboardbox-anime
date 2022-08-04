namespace CardboardBox.Anime.Vrv
{
	public interface IVrvConfig
	{
		string ResourceList { get; }
		Dictionary<string, string> Query { get; }
	}

	public class VrvConfig : IVrvConfig
	{
		public string ResourceList { get; set; } = "";

		public Dictionary<string, string> Query { get; set; } = new();
	}
}
