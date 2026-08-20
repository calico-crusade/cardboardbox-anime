namespace CardboardBox.Epub;

public interface IEpubBuilderPageBreaks
{
	IEpubBuilder EnableApproximatePageBreaks(int wordsPerPage = 350);
}

public partial class EpubBuilder
{
	private EpubPageBreakGenerator? _pageBreakGenerator;

	private bool ApproximatePageBreaksEnabled => _pageBreakGenerator is not null;

	public IEpubBuilder EnableApproximatePageBreaks(int wordsPerPage = 350)
	{
		_pageBreakGenerator = new EpubPageBreakGenerator(wordsPerPage);
		return this;
	}

	internal async Task AddPageFile(string name, Stream stream)
	{
		if (!ApproximatePageBreaksEnabled)
		{
			await AddFile(name, stream, FileType.Page);
			return;
		}

		using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
		using var page = _pageBreakGenerator!
			.AddToXhtmlPage(await reader.ReadToEndAsync(), name)
			.ToStream();
		await AddFile(name, page, FileType.Page);
	}

	private string GeneratePageList()
	{
		var pageBreaks = _pageBreakGenerator?.PageBreaks;
		if (pageBreaks is null || pageBreaks.Count == 0)
			return string.Empty;

		var pages = string.Join(
			Environment.NewLine,
			pageBreaks.Select(page =>
				$"      <li><a href=\"{HtmlEntity.Entitize(page.Href)}\">{page.PageNumber}</a></li>"));

		return $"""

  <nav epub:type="page-list" hidden="hidden">
    <h2>Pages</h2>
    <ol>
{pages}
    </ol>
  </nav>
""";
	}
}

public sealed class EpubPageBreakGenerator
{
	private readonly List<EpubPageBreak> _pageBreaks = [];
	private int _nextPageNumber = 1;

	public int WordsPerPage { get; }
	public IReadOnlyList<EpubPageBreak> PageBreaks => _pageBreaks;

	public EpubPageBreakGenerator(int wordsPerPage = 350)
	{
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(wordsPerPage);

        WordsPerPage = wordsPerPage;
	}

	public string AddToXhtmlPage(string html, string pageHref)
	{
		var doc = new HtmlDocument
		{
			OptionFixNestedTags = true,
			OptionAutoCloseOnEnd = true,
			OptionWriteEmptyNodes = false,
			OptionOutputAsXml = true,
		};

		doc.LoadHtml(html);

		var root = doc.DocumentNode.SelectSingleNode("//*[local-name()='html']");
		if (root is not null && string.IsNullOrWhiteSpace(root.GetAttributeValue("xmlns:epub", "")))
			root.SetAttributeValue("xmlns:epub", "http://www.idpf.org/2007/ops");

		var body = doc.DocumentNode.SelectSingleNode("//*[local-name()='body']");
		if (body is null)
			return html;

		var paragraphs = body
			.Descendants()
			.Where(node =>
				node.NodeType == HtmlNodeType.Element &&
				node.Name.Equals("p", StringComparison.OrdinalIgnoreCase) &&
				CountWords(node.InnerText) > 0)
			.ToArray();

		if (paragraphs.Length == 0)
			return html;

		var normalizedHref = pageHref.Replace('\\', '/').TrimStart('/');
		var wordsSinceBreak = 0;
		var insertedAtLeastOne = false;

		foreach (var paragraph in paragraphs)
		{
			var wordCount = CountWords(paragraph.InnerText);

			if (!insertedAtLeastOne || wordsSinceBreak >= WordsPerPage)
			{
				var pageNumber = _nextPageNumber++;
				var pageId = $"page-{pageNumber}";
				var pageBreak = HtmlNode.CreateNode(
					$"""<span epub:type="pagebreak" id="{pageId}" title="{pageNumber}"></span>""");

				paragraph.ParentNode.InsertBefore(pageBreak, paragraph);
				_pageBreaks.Add(new EpubPageBreak(pageNumber, $"{normalizedHref}#{pageId}"));

				wordsSinceBreak = 0;
				insertedAtLeastOne = true;
			}

			wordsSinceBreak += wordCount;
		}

		return doc.DocumentNode.OuterHtml;
	}

	private static int CountWords(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return 0;

		return Regex.Matches(text, @"[\p{L}\p{N}]+(?:['’\-][\p{L}\p{N}]+)?").Count;
	}
}

public sealed record EpubPageBreak(int PageNumber, string Href);
