using CardboardBox.LightNovel.Core;
using CommandLine;

namespace CardboardBox.Anime.Cli.Verbs;

[Verb("clean-rr", HelpText = "Clean RR data for patreon sources")]
public class CleanRROptions
{
	[Option('i', "id", HelpText = "Id to clean", Required = true)]
	public int Id { get; set; }
}

internal class CleanRRVerb(
	ILnDbService _db,
	ILogger<CleanRRVerb> logger) : BooleanVerb<CleanRROptions>(logger)
{
	public static class EpubHtmlCleaner
	{
		private static readonly HashSet<char> RemoveCompletely =
		[
			'\u200B', // Zero Width Space
			'\u200C', // Zero Width Non-Joiner
			'\u200D', // Zero Width Joiner
			'\u2060', // Word Joiner
			'\uFEFF', // Zero Width No-Break Space / BOM
		];

		private static readonly HashSet<char> ReplaceWithSpace =
		[
			'\u00A0', // Non-breaking space
		];

		private static readonly HashSet<char> InvisibleChars =
		[
			'\u200B', // Zero Width Space
			'\u200C', // Zero Width Non-Joiner
			'\u200D', // Zero Width Joiner
			'\u2060', // Word Joiner
			'\uFEFF', // Zero Width No-Break Space / BOM
			'\u00A0', // Non-breaking space
		];

		public static string CleanInvisibleTextCharacters(string html)
		{
			if (string.IsNullOrEmpty(html))
				return html;

			var doc = new HtmlDocument
			{
				OptionFixNestedTags = true,
				OptionAutoCloseOnEnd = true,
				OptionWriteEmptyNodes = true,
			};

			doc.LoadHtml(html);

			CleanNode(doc.DocumentNode);

			return doc.DocumentNode.OuterHtml;
		}

		public static string RemoveHiddenParagraphs(string html)
		{
			if (string.IsNullOrWhiteSpace(html))
				return html;

			var doc = new HtmlDocument
			{
				OptionFixNestedTags = true,
				OptionAutoCloseOnEnd = true,
				OptionWriteEmptyNodes = true,
			};

			doc.LoadHtml(html);

			var paragraphs = doc.DocumentNode
				.SelectNodes("//p")
				?.ToArray();

			if (paragraphs is null)
				return doc.DocumentNode.OuterHtml;

			foreach (var paragraph in paragraphs)
			{
				if (IsHiddenParagraph(paragraph))
					paragraph.Remove();
			}

			return doc.DocumentNode.OuterHtml;
		}
		
		public static string ReplaceHiddenParagraphsWithSectionBreak(string html)
		{
			if (string.IsNullOrWhiteSpace(html))
				return html;

			var doc = new HtmlDocument
			{
				OptionFixNestedTags = true,
				OptionAutoCloseOnEnd = true,
				OptionWriteEmptyNodes = true,
			};

			doc.LoadHtml(html);

			var paragraphs = doc.DocumentNode
				.SelectNodes("//p")
				?.ToArray();

			if (paragraphs is null)
				return doc.DocumentNode.OuterHtml;

			foreach (var paragraph in paragraphs)
			{
				if (!IsHiddenParagraph(paragraph))
					continue;

				var replacement = HtmlNode.CreateNode("""<p class="section-break">* * *</p>""");
				paragraph.ParentNode.ReplaceChild(replacement, paragraph);
			}

			return doc.DocumentNode.OuterHtml;
		}

		private static void CleanNode(HtmlNode node)
		{
			foreach (var child in node.ChildNodes.ToArray())
			{
				if (child.NodeType == HtmlNodeType.Text)
				{
					CleanTextNode(child);
					continue;
				}

				if (child.NodeType == HtmlNodeType.Element)
				{
					// Usually you do not want to alter script/style text.
					if (child.Name.Equals("script", StringComparison.OrdinalIgnoreCase) ||
						child.Name.Equals("style", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					CleanNode(child);
				}
			}
		}

		private static void CleanTextNode(HtmlNode textNode)
		{
			var original = textNode.InnerHtml;

			if (string.IsNullOrEmpty(original))
				return;

			var decoded = WebUtility.HtmlDecode(original);
			var cleaned = CleanText(decoded);

			if (cleaned == decoded)
				return;

			textNode.InnerHtml = HtmlEntity.Entitize(cleaned);
		}

		private static string CleanText(string text)
		{
			var builder = new StringBuilder(text.Length);

			foreach (var c in text)
			{
				if (RemoveCompletely.Contains(c))
					continue;

				if (ReplaceWithSpace.Contains(c))
				{
					builder.Append(' ');
					continue;
				}

				builder.Append(c);
			}

			return builder.ToString();
		}

		private static bool IsHiddenParagraph(HtmlNode paragraph)
		{
			// If the paragraph contains meaningful child elements, keep it.
			// Examples: <img>, <br>, <span class="...">text</span>, etc.
			foreach (var child in paragraph.ChildNodes)
			{
				if (child.NodeType == HtmlNodeType.Element)
				{
					// A <br> alone should still count as empty/hidden.
					if (child.Name.Equals("br", StringComparison.OrdinalIgnoreCase))
						continue;

					// For inline containers, check their text recursively.
					if (child.Name.Equals("span", StringComparison.OrdinalIgnoreCase) ||
						child.Name.Equals("i", StringComparison.OrdinalIgnoreCase) ||
						child.Name.Equals("b", StringComparison.OrdinalIgnoreCase) ||
						child.Name.Equals("em", StringComparison.OrdinalIgnoreCase) ||
						child.Name.Equals("strong", StringComparison.OrdinalIgnoreCase))
					{
						if (!IsInvisibleText(child.InnerText))
							return false;

						continue;
					}

					return false;
				}
			}

			return IsInvisibleText(paragraph.InnerText);
		}

		private static bool IsInvisibleText(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return true;

			text = WebUtility.HtmlDecode(text);

			foreach (var c in text)
			{
				if (char.IsWhiteSpace(c))
					continue;

				if (InvisibleChars.Contains(c))
					continue;

				return false;
			}

			return true;
		}

	}

	public static async Task<Notes[]> GetNotes()
	{
		const string path = "rr.json";
		if (!File.Exists(path)) return [];

		using var io = File.OpenRead(path);
		return await JsonSerializer.DeserializeAsync<Notes[]>(io) ?? [];
	}

	public async Task ProcessChapter(string[] notes, Page page)
	{
		const StringComparison comp = StringComparison.InvariantCultureIgnoreCase;

		void ClearNode(HtmlNode node)
		{
			if (node == null) return;

			var it = node.InnerText?.HTMLDecode().Trim();

			if (!string.IsNullOrEmpty(it) && notes.Any(t => it.Equals(t, comp)))
			{
				node.Remove();
				return;
			}

			if (node.ChildNodes == null || node.ChildNodes.Count == 0) return;

			foreach (var child in node.ChildNodes?.ToArray() ?? [])
				ClearNode(child);
		}

		void RemoveCode(HtmlNode node)
		{
			if (node == null) return;

			if (node.InnerHtml is not null &&
				node.InnerHtml.Contains("<pre>", comp))
				node.InnerHtml = node.InnerHtml.Replace("<pre>", "").Replace("</pre>", "");

			var codes = node.SelectNodes("//code")?.ToArray() ?? [];
			if (codes is null) return;

			foreach(var code in codes)
			{
				var inner = code.InnerHtml;
				if (string.IsNullOrEmpty(inner))
				{
					code.Remove();
					continue;
				}

				var split = HtmlEntity.DeEntitize(inner)
					.Split(["\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
					.Select(t => t.Trim())
					.Where(t => !string.IsNullOrEmpty(t));

				foreach(var line in split)
				{
					var p = HtmlNode.CreateNode("<p></p>");
					p.InnerHtml = WebUtility.HtmlEncode(line);
					code.ParentNode.InsertBefore(p, code);

					code.ParentNode.InsertBefore(HtmlTextNode.CreateNode(Environment.NewLine), code);
				}

				code.Remove();
			}

		}

		var before = page.Content;
		var doc = new HtmlDocument();
		doc.LoadHtml(before);
		ClearNode(doc.DocumentNode);
		RemoveCode(doc.DocumentNode);

		page.Content = EpubHtmlCleaner.CleanInvisibleTextCharacters(
			EpubHtmlCleaner.RemoveHiddenParagraphs(
				doc.DocumentNode.OuterHtml));
		await _db.Pages.Update(page);
	}

	public override async Task<bool> Execute(CleanRROptions options, CancellationToken token)
	{
		var notes = await GetNotes();
		var antiPiracy = notes
			.Where(n => n.Type == Notes.ANTI_PIRACY && !string.IsNullOrEmpty(n.Text))
			.Select(t => t.Text!)
			.ToArray();
		var id = options.Id;
		var pages = await _db.Pages.Paginate(id, 1, 99999);
		await Parallel.ForEachAsync(pages.Results, async (p, ct) =>
		{
			await ProcessChapter(antiPiracy, p);
		});
		_logger.LogInformation("Finished processing novel with id {Id}", id);

		return true;
	}

	public record class Notes(
		[property: JsonPropertyName("insertType")] string Type,
		[property: JsonPropertyName("text")] string? Text,
		[property: JsonPropertyName("chapters")] string[] Chapters,
		[property: JsonPropertyName("summary")] string? Summary)
	{
		public const string ANTI_PIRACY = "antiPiracy";

		public const string AUTHOR_NOTE = "authorNote";
	}
}
