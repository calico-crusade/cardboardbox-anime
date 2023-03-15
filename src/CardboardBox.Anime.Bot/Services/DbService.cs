namespace CardboardBox.Anime.Bot.Services;

public interface IDbService
{
	ILookupDbService Lookup { get; }

	IGptDbService Gpt { get; }
}

public class DbService : IDbService
{
	public ILookupDbService Lookup { get; }

	public IGptDbService Gpt { get; }

	public DbService(ILookupDbService lookup, IGptDbService gpt)
	{
		Lookup = lookup;
		Gpt = gpt;
	}
}
