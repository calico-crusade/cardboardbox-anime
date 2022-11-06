namespace CardboardBox.Manga.MangaDex.Models
{
	public class MangaDexCollection<T>: MangaDexRoot<List<T>>
	{
		[JsonPropertyName("limit")]
		public int Limit { get; set; }

		[JsonPropertyName("offset")]
		public int Offset { get; set; }

		[JsonPropertyName("total")]
		public int Total { get; set; }
	}

	public class MangaDexRoot<T> where T : new()
	{
		[JsonPropertyName("result")]
		public string Result { get; set; } = string.Empty;

		[JsonPropertyName("response")]
		public string Response { get; set; } = string.Empty;

		[JsonPropertyName("data")]
		public T Data { get; set; } = new();
	}

	public abstract class MangaDexModel
	{
		[JsonPropertyName("id")]
		public virtual string Id { get; set; } = string.Empty;

		[JsonPropertyName("type")]
		public virtual string Type { get; set; } = string.Empty;
	}

	public abstract class MangaDexModel<T> : MangaDexModel where T : new()
	{
		[JsonPropertyName("attributes")]
		public virtual T Attributes { get; set; } = new();
	}
}