using iText.Html2pdf;

namespace CardboardBox.LightNovel.Core
{
	using Anime.Database;
	using Epub;

	public interface IPdfService
	{
		Task ToPdf(string bookId, int chunks = 100);
	}

	public class PdfService : IPdfService
	{
		private readonly IChapterDbService _db;
		private readonly ILogger _logger;

		public PdfService(
			IChapterDbService db,
			ILogger<PdfService> logger)
		{
			_db = db;
			_logger = logger;
		}

		public async Task ToPdf(string bookId, int chunks = 100)
		{
			var chapToHtml = (DbChapter chap) => $"<h2 style=\"page-break-before: always\">{chap.Chapter}</h2><hr />{chap.Content}";

			int page = 1;
			while (true)
			{
				var (_, chaps) = await _db.Chapters(bookId, page, chunks);

				if (chaps.Length == 0) break;
				var fc = chaps.First();
				var filename = $"{fc.Book}-Vol-{page}.pdf".PurgePathChars();

				var html = $"<h1>{fc.Book}</h1><hr />{string.Join("<br />", chaps.Select(chapToHtml))}";

				using var io = File.Create(filename);
				HtmlConverter.ConvertToPdf(html, io);
				page++;
			}
		}
	}
}
