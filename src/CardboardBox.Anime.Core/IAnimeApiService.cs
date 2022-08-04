namespace CardboardBox.Anime.Core
{
	using Models;

	public interface IAnimeApiService
	{
		IAsyncEnumerable<Anime> All();
	}
}
