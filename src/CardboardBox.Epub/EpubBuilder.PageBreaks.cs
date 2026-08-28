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

public sealed partial class EpubPageBreakGenerator
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
		if (root is not null && string.IsNullOrWhiteSpace(root.GetAttributeValue("xmlns:epub", string.Empty)))
			root.SetAttributeValue("xmlns:epub", "http://www.idpf.org/2007/ops");

		var body = doc.DocumentNode.SelectSingleNode("//*[local-name()='body']");
		if (body is null)
			return html;

		foreach (var existingPageBreak in body.Descendants().Where(IsPageBreak).ToArray())
			existingPageBreak.Remove();

		var normalizedHref = pageHref.Replace('\\', '/').TrimStart('/');
		var wordsSinceBreak = 0;
		var insertedAtLeastOne = false;
		var textNodes = body
			.Descendants()
			.Where(node => node.NodeType == HtmlNodeType.Text && !ShouldIgnore(node))
			.Cast<HtmlTextNode>()
			.ToArray();

		foreach (var textNode in textNodes)
		{
			var text = textNode.Text;
			var words = WordRegex().Matches(text);
			if (words.Count == 0) continue;

			var parent = textNode.ParentNode;
			var position = 0;
			foreach (Match word in words)
			{
				if (!insertedAtLeastOne || wordsSinceBreak >= WordsPerPage)
				{
					if (word.Index > position)
						parent.InsertBefore(doc.CreateTextNode(text[position..word.Index]), textNode);

					var pageNumber = _nextPageNumber++;
					var pageId = $"page-{pageNumber}";
					parent.InsertBefore(HtmlNode.CreateNode(
						$"""<span epub:type="pagebreak" id="{pageId}" title="{pageNumber}"></span>"""), textNode);
					_pageBreaks.Add(new EpubPageBreak(pageNumber, $"{normalizedHref}#{pageId}"));

					position = word.Index;
					wordsSinceBreak = 0;
					insertedAtLeastOne = true;
				}

				wordsSinceBreak++;
			}

			if (position == 0) continue;
			parent.InsertBefore(doc.CreateTextNode(text[position..]), textNode);
			textNode.Remove();
		}

		return doc.DocumentNode.OuterHtml;
	}

	private static bool ShouldIgnore(HtmlNode node)
	{
		return node.Ancestors().Any(ancestor =>
			ancestor.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
			ancestor.Name.Equals("style", StringComparison.OrdinalIgnoreCase) ||
			IsPageBreak(ancestor));
	}

	private static bool IsPageBreak(HtmlNode node) => node.GetAttributeValue("epub:type", string.Empty)
		.Split(' ', StringSplitOptions.RemoveEmptyEntries)
		.Contains("pagebreak", StringComparer.OrdinalIgnoreCase);

	[GeneratedRegex(@"[\p{L}\p{N}]+(?:['\u2019\-][\p{L}\p{N}]+)?")]
	private static partial Regex WordRegex();
}

public sealed record EpubPageBreak(int PageNumber, string Href);
