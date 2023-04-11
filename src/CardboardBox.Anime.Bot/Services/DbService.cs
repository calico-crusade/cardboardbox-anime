namespace CardboardBox.Anime.Bot.Services;

public interface IDbService
{
	ILookupDbService Lookup { get; }

	IGptDbService Gpt { get; }

	INsfwConfigDbService Nsfw { get; }
}

public class DbService : IDbService
{
	public ILookupDbService Lookup { get; }

	public IGptDbService Gpt { get; }

	public INsfwConfigDbService Nsfw { get; }

	public DbService(
		ILookupDbService lookup, 
		IGptDbService gpt, 
		INsfwConfigDbService nsfw)
	{
		Lookup = lookup;
		Gpt = gpt;
		Nsfw = nsfw;
	}
}
