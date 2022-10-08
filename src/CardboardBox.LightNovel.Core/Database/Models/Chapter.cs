namespace CardboardBox.LightNovel.Core
{
	public class Chapter : BookBase
	{
		[JsonPropertyName("bookId")]
		public long BookId { get; set; }
	}
}
