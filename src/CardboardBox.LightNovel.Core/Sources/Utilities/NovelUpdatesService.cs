namespace CardboardBox.LightNovel.Core.Sources.Utilities;

public interface INovelUpdatesService
{
	Task<TempSeriesInfo?> Series(string url);
}

public class NovelUpdatesService : INovelUpdatesService
{
	private readonly IApiService _api;

	public NovelUpdatesService(IApiService api)
	{
		_api = api;
	}

	public async Task<TempSeriesInfo?> Series(string url)
	{
		var doc = await _api.GetHtml(url);
		if (doc == null) return null;

		string? title = doc.InnerText("//div[@class='seriestitlenu']"),
				description = doc.InnerHtml("//div[@id='editdescription']"),
				image = doc.Attribute("//div[@class='seriesimg']/img", "src");

		if (string.IsNullOrWhiteSpace(title)) return null;

		var authors = doc
			.DocumentNode
			.SelectNodes("//div[@id='showauthors']/a")?
			.Select(t => t.InnerText.HTMLDecode())
			.ToArray() ?? Array.Empty<string>();

		var tags = doc
			.DocumentNode
			.SelectNodes("//div[@id='showtags']/a[@id='etagme']")?
			.Select(t => t.InnerText.HTMLDecode())
			.ToArray() ?? Array.Empty<string>();

		var genres = doc
			.DocumentNode
			.SelectNodes("//div[@id='seriesgenre']/a[@class='genre']")?
			.Select(t => t.InnerText.HTMLDecode())
			.ToArray() ?? Array.Empty<string>();

		return new TempSeriesInfo(title, description, authors, image, null, genres, tags);
	}
}
