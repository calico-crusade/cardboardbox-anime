namespace CardboardBox.LightNovel.Core;

using Anime.Database;
using Anime.Database.Mapping;
using Database;
using Sources;
using Sources.Utilities;
using Sources.Utilities.FlareSolver;
using Sources.ZirusSource;

public static class Extensions
{
	public static async IAsyncEnumerable<DbChapter> DbChapters(this ISourceService src, string firstUrl)
	{
		DbChapter? last = null;
		await foreach (var chapter in src.Chapters(firstUrl))
			yield return last = FromChapter(chapter, last);
	}

	public static IEnumerable<T> AReverse<T>(this IEnumerable<T> source)
	{
		return source.Reverse();
	}

	public static DbChapter FromChapter(SourceChapter chapter, DbChapter? last)
	{
		return new DbChapter
		{
			HashId = chapter.Url.MD5Hash(),
			BookId = chapter.BookTitle.MD5Hash(),
			Book = chapter.BookTitle,
			Chapter = chapter.ChapterTitle,
			Content = chapter.Content,
			Url = chapter.Url,
			NextUrl = chapter.NextUrl,
			Ordinal = (last?.Ordinal ?? 0) + 1
		};
	}

	public static string Join(this IEnumerable<HtmlNode> nodes, bool checkWs = false)
	{
		try
		{
			var doc = new HtmlDocument();
			foreach (var node in nodes)
				if (!checkWs || !string.IsNullOrWhiteSpace(node.InnerText))
					doc.DocumentNode.AppendChild(node);

			return doc.DocumentNode.InnerHtml.Trim();
		}
		catch
		{
			return string.Empty;
		}
	}

	public static void CleanupNode(this HtmlNode parent)
	{
        parent.SelectNodes("./noscript")?
            .ToList()
            .ForEach(t => t.Remove());

        parent.SelectNodes("./img")?
            .ToList()
            .ForEach(t =>
            {
                foreach (var attr in t.Attributes.ToArray())
                    if (attr.Name != "src" && attr.Name != "alt")
                        t.Attributes.Remove(attr);
            });

        if (parent.ChildNodes.Count == 0)
        {
            parent.Remove();
            return;
        }

        if (parent.ParentNode != null &&
			parent.ChildNodes.Count == 1 && 
			parent.FirstChild.Name == "img")
        {
            parent.ParentNode.InsertBefore(parent.FirstChild, parent);
            parent.Remove();
            return;
        }
    }

	public static IServiceCollection AddLightNovel(this IServiceCollection services)
	{
		MapConfig.AddMap(c =>
		{
			c.ForEntity<Book>()
			 .ForEntity<Chapter>()
			 .ForEntity<Page>()
			 .ForEntity<Series>()
			 .ForEntity<ChapterPage>();
		});

		return services
			.AddTransient<ILnpSourceService, LnpSourceService>()
			.AddTransient<IShSourceService, ShSourceService>()
			.AddTransient<IReLibSourceService, ReLibSourceService>()
			.AddTransient<INewRelibrarySourceService, NewRelibrarySourceService>()
			.AddTransient<ILntSourceService, LntSourceService>()
			.AddTransient<INyxSourceService, NyxSourceService>()
			.AddTransient<IZirusApiService, ZirusApiService>()
			.AddTransient<IZirusMusingsSourceService, ZirusMusingsSourceService>()
			.AddTransient<INncSourceService, NncSourceService>()
			.AddTransient<IBakaPervertSourceService, BakaPervertSourceService>()
			.AddTransient<IFanTransSourceService, FanTransSourceService>()
			.AddTransient<IMagicHouseSourceService, MagicHouseSourceService>()
			.AddTransient<IHeadCanonTLSourceService, HeadCanonTLSourceService>()
			.AddTransient<IVampiramtlSourceService, VampiramtlSourceService>()
			.AddTransient<IRoyalRoadSourceService, RoyalRoadSourceService>()
			.AddTransient<IStorySeedlingSourceService, StorySeedlingSourceService>()
			.AddTransient<ICardboardTranslationsSourceService, CardboardTranslationsSourceService>()
			.AddTransient<INovelBinSourceService, NovelBinSourceService>()

			.AddTransient<INovelUpdatesService, NovelUpdatesService>()

			.AddTransient<IPurgeUtils, PurgeUtils>()
			.AddTransient<IAITranslatorService, AITranslatorService>()
			.AddTransient<ISmartReaderService, SmartReaderService>()

			.AddTransient<IOldLnApiService, OldLnApiService>()
			.AddTransient<INovelApiService, NovelApiService>()
			.AddTransient<INovelEpubService, NovelEpubService>()
			.AddTransient<IPdfService, PdfService>()
			.AddTransient<IMarkdownService, MarkdownService>()
			
			.AddTransient<IDbBookService, DbBookService>()
			.AddTransient<IDbChapterService, DbChapterService>()
			.AddTransient<IDbPageService, DbPageService>()
			.AddTransient<IDbSeriesService, DbSeriesService>()
			.AddTransient<IDbChapterPageService, DbChapterPageService>()
			
			.AddTransient<ILnDbService, LnDbService>()
			
			.AddFlareSolver();
	}
}
