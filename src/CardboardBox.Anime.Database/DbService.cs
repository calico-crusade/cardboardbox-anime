namespace CardboardBox.Anime.Database;

public interface IDbService
{
	IAnimeDbService Anime { get; }
	IListDbService Lists { get; }
	IListMapDbService ListMaps { get; }
	IProfileDbService Profiles { get; }
	IAiRequestDbService AiRequests { get; }
	IMangaDbService Manga { get; }
	IChapterDbService Novels { get; }
}

public class DbService : IDbService
{
	public IAnimeDbService Anime { get; }
	public IListDbService Lists { get; }
	public IListMapDbService ListMaps { get; }
	public IProfileDbService Profiles { get; }
	public IMangaDbService Manga { get; }
	public IAiRequestDbService AiRequests { get; }
	public IChapterDbService Novels { get; }

	public DbService(IAnimeDbService anime,
		IListDbService lists, 
		IListMapDbService listMaps,
		IProfileDbService profiles,
		IAiRequestDbService aiRequests,
		IMangaDbService manga,
		IChapterDbService novels)
	{
		Anime = anime;
		Lists = lists;
		ListMaps = listMaps;
		Profiles = profiles;
		AiRequests = aiRequests;
		Manga = manga;
		Novels = novels;
	}
}
