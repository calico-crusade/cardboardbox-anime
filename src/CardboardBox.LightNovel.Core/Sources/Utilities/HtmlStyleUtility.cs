using ExCSS;

namespace CardboardBox.LightNovel.Core.Sources.Utilities;

public static class HtmlStyleUtility
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    public static int InlineDocumentStyles(HtmlDocument doc)
    {
        var styles = doc.DocumentNode.SelectNodes("//style")?.ToArray() ?? [];
        if (styles.Length == 0) return 0;

        var parser = new StylesheetParser();
        var protectedInlineStyles = doc.DocumentNode
            .Descendants()
            .Where(t => t.NodeType == HtmlNodeType.Element)
            .ToDictionary(
                t => t,
                t => ParseStyleAttribute(t.GetAttributeValue("style", string.Empty)).Keys.ToHashSet(NameComparer));
        var count = 0;

        foreach (var style in styles)
        {
            var css = style.InnerText;
            if (string.IsNullOrWhiteSpace(css)) continue;

            var sheet = parser.Parse(css);
            foreach (var rule in sheet.StyleRules)
                count += InlineRule(doc, rule, protectedInlineStyles);
        }

        return count;
    }

    public static int RemoveHiddenBodyElements(HtmlDocument doc)
    {
        var body = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
        return RemoveHiddenElements(body);
    }

    public static void InlineStylesAndRemoveHidden(HtmlDocument doc)
    {
        InlineDocumentStyles(doc);
        RemoveHiddenBodyElements(doc);
    }

    private static int InlineRule(
        HtmlDocument doc,
        IStyleRule rule,
        Dictionary<HtmlNode, HashSet<string>> protectedInlineStyles)
    {
        var declarations = rule.Style.Declarations
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .ToArray();
        if (declarations.Length == 0) return 0;

        var count = 0;
        foreach (var selector in SplitSelectorList(rule.SelectorText))
        {
            foreach (var node in SelectNodes(doc, selector))
            {
                protectedInlineStyles.TryGetValue(node, out var protectedStyles);
                MergeStyle(node, declarations, protectedStyles ?? []);
                count++;
            }
        }

        return count;
    }

    private static IEnumerable<HtmlNode> SelectNodes(HtmlDocument doc, string selector)
    {
        var parts = ParseSelector(selector);
        if (parts.Length == 0) yield break;

        foreach (var node in doc.DocumentNode.Descendants().Where(t => t.NodeType == HtmlNodeType.Element))
        {
            if (MatchesSelector(node, parts))
                yield return node;
        }
    }

    private static int RemoveHiddenElements(HtmlNode node)
    {
        var count = 0;
        foreach (var child in node.ChildNodes.ToArray())
        {
            if (child.NodeType != HtmlNodeType.Element) continue;

            if (IsDisplayNone(child))
            {
                child.Remove();
                count++;
                continue;
            }

            count += RemoveHiddenElements(child);
        }

        return count;
    }

    private static bool IsDisplayNone(HtmlNode node)
    {
        var style = ParseStyleAttribute(node.GetAttributeValue("style", string.Empty));
        return style.TryGetValue("display", out var display) &&
               string.Equals(TrimImportant(display), "none", StringComparison.OrdinalIgnoreCase);
    }

    private static void MergeStyle(
        HtmlNode node,
        IEnumerable<IProperty> declarations,
        IReadOnlySet<string> protectedStyles)
    {
        var styles = ParseStyleAttribute(node.GetAttributeValue("style", string.Empty));

        foreach (var declaration in declarations)
        {
            var value = declaration.Value;
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (!declaration.IsImportant && protectedStyles.Contains(declaration.Name)) continue;
            if (!declaration.IsImportant &&
                styles.TryGetValue(declaration.Name, out var existing) &&
                existing.Contains("!important", StringComparison.OrdinalIgnoreCase)) continue;

            styles[declaration.Name] = declaration.IsImportant && !value.Contains("!important", StringComparison.OrdinalIgnoreCase)
                ? $"{value} !important"
                : value;
        }

        node.SetAttributeValue("style", string.Join(" ", styles.Select(t => $"{t.Key}: {t.Value};")));
    }

    private static Dictionary<string, string> ParseStyleAttribute(string style)
    {
        var output = new Dictionary<string, string>(NameComparer);
        if (string.IsNullOrWhiteSpace(style)) return output;

        foreach (var item in SplitOutsideBrackets(style, ';'))
        {
            var index = item.IndexOf(':');
            if (index <= 0) continue;

            var name = item[..index].Trim();
            var value = item[(index + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) continue;

            output[name] = value;
        }

        return output;
    }

    private static bool MatchesSelector(HtmlNode node, SelectorPart[] parts)
    {
        return MatchesSelector(node, parts, parts.Length - 1);
    }

    private static bool MatchesSelector(HtmlNode? node, SelectorPart[] parts, int index)
    {
        if (node is null || node.NodeType != HtmlNodeType.Element || !MatchesPart(node, parts[index]))
            return false;

        if (index == 0)
            return true;

        if (parts[index].Combinator == CssCombinator.Child)
            return MatchesSelector(node.ParentNode, parts, index - 1);

        var parent = node.ParentNode;
        while (parent is not null)
        {
            if (MatchesSelector(parent, parts, index - 1))
                return true;

            parent = parent.ParentNode;
        }

        return false;
    }

    private static bool MatchesPart(HtmlNode node, SelectorPart part)
    {
        if (!string.IsNullOrWhiteSpace(part.Tag) &&
            !string.Equals(node.Name, part.Tag, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(part.Id) &&
            !string.Equals(node.GetAttributeValue("id", string.Empty), part.Id, StringComparison.Ordinal))
            return false;

        var classes = GetClasses(node);
        if (part.Classes.Any(t => !classes.Contains(t)))
            return false;

        foreach (var attr in part.Attributes)
        {
            var value = node.Attributes[attr.Name]?.Value;
            if (value is null) return false;

            if (attr.Value is not null && !MatchesAttribute(value, attr))
                return false;
        }

        return true;
    }

    private static bool MatchesAttribute(string value, CssAttribute attr)
    {
        return attr.Operator switch
        {
            "=" => string.Equals(value, attr.Value, StringComparison.Ordinal),
            "~=" => value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(attr.Value),
            "^=" => value.StartsWith(attr.Value ?? string.Empty, StringComparison.Ordinal),
            "$=" => value.EndsWith(attr.Value ?? string.Empty, StringComparison.Ordinal),
            "*=" => value.Contains(attr.Value ?? string.Empty, StringComparison.Ordinal),
            "|=" => value == attr.Value || value.StartsWith($"{attr.Value}-", StringComparison.Ordinal),
            _ => true
        };
    }

    private static HashSet<string> GetClasses(HtmlNode node)
    {
        return node
            .GetAttributeValue("class", string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static SelectorPart[] ParseSelector(string selector)
    {
        selector = StripUnsupportedSelectorParts(selector);
        if (string.IsNullOrWhiteSpace(selector)) return [];

        var tokens = TokenizeSelector(selector);
        var parts = new List<SelectorPart>();
        var combinator = CssCombinator.Descendant;

        foreach (var token in tokens)
        {
            if (token == ">")
            {
                combinator = CssCombinator.Child;
                continue;
            }

            var part = ParsePart(token);
            if (part is null) return [];

            part.Combinator = combinator;
            parts.Add(part);
            combinator = CssCombinator.Descendant;
        }

        return parts.ToArray();
    }

    private static SelectorPart? ParsePart(string token)
    {
        var part = new SelectorPart();
        var index = 0;

        while (index < token.Length)
        {
            var current = token[index];
            if (current == '.')
            {
                index++;
                var value = ReadIdentifier(token, ref index);
                if (string.IsNullOrWhiteSpace(value)) return null;
                part.Classes.Add(value);
                continue;
            }

            if (current == '#')
            {
                index++;
                part.Id = ReadIdentifier(token, ref index);
                if (string.IsNullOrWhiteSpace(part.Id)) return null;
                continue;
            }

            if (current == '[')
            {
                var end = token.IndexOf(']', index + 1);
                if (end == -1) return null;

                var attr = ParseAttribute(token[(index + 1)..end]);
                if (attr is null) return null;

                part.Attributes.Add(attr);
                index = end + 1;
                continue;
            }

            if (current == '*')
            {
                index++;
                continue;
            }

            var tag = ReadIdentifier(token, ref index);
            if (string.IsNullOrWhiteSpace(tag)) return null;
            part.Tag = tag;
        }

        return part;
    }

    private static CssAttribute? ParseAttribute(string value)
    {
        string[] operators = ["~=", "^=", "$=", "*=", "|=", "="];
        var op = operators.FirstOrDefault(value.Contains);
        if (op is null)
            return new CssAttribute(value.Trim(), null, null);

        var parts = value.Split(op, 2);
        if (parts.Length != 2) return null;

        return new CssAttribute(
            parts[0].Trim(),
            op,
            parts[1].Trim().Trim('\'', '"'));
    }

    private static string ReadIdentifier(string text, ref int index)
    {
        var start = index;
        while (index < text.Length)
        {
            var c = text[index];
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or ':')
            {
                index++;
                continue;
            }

            break;
        }

        return text[start..index];
    }

    private static string StripUnsupportedSelectorParts(string selector)
    {
        var pseudo = FindPseudoSelectorIndex(selector);
        if (pseudo >= 0)
            return string.Empty;

        if (selector.Contains('+') || selector.Contains('~'))
            return string.Empty;

        return selector.Trim();
    }

    private static int FindPseudoSelectorIndex(string selector)
    {
        var bracketDepth = 0;
        var quote = '\0';

        for (var i = 0; i < selector.Length; i++)
        {
            var c = selector[i];
            if (quote != '\0')
            {
                if (c == quote) quote = '\0';
                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
                continue;
            }

            if (c == '[') bracketDepth++;
            if (c == ']') bracketDepth--;

            if (bracketDepth == 0 && c == ':')
                return i;
        }

        return -1;
    }

    private static string[] TokenizeSelector(string selector)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var bracketDepth = 0;

        foreach (var c in selector)
        {
            if (c == '[') bracketDepth++;
            if (c == ']') bracketDepth--;

            if (bracketDepth == 0 && c == '>')
            {
                AddToken();
                tokens.Add(">");
                continue;
            }

            if (bracketDepth == 0 && char.IsWhiteSpace(c))
            {
                AddToken();
                continue;
            }

            current.Append(c);
        }

        AddToken();
        return tokens.ToArray();

        void AddToken()
        {
            if (current.Length == 0) return;

            var token = current.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(token))
                tokens.Add(token);
            current.Clear();
        }
    }

    private static IEnumerable<string> SplitSelectorList(string selector)
    {
        return SplitOutsideBrackets(selector, ',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t));
    }

    private static IEnumerable<string> SplitOutsideBrackets(string text, char separator)
    {
        var current = new StringBuilder();
        var depth = 0;
        var quote = '\0';

        foreach (var c in text)
        {
            if (quote != '\0')
            {
                current.Append(c);
                if (c == quote) quote = '\0';
                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
                current.Append(c);
                continue;
            }

            if (c is '(' or '[') depth++;
            if (c is ')' or ']') depth--;

            if (depth == 0 && c == separator)
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(c);
        }

        yield return current.ToString();
    }

    private static string TrimImportant(string value)
    {
        return value.Replace("!important", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private enum CssCombinator
    {
        Descendant,
        Child
    }

    private sealed class SelectorPart
    {
        public string? Tag { get; set; }
        public string? Id { get; set; }
        public List<string> Classes { get; } = [];
        public List<CssAttribute> Attributes { get; } = [];
        public CssCombinator Combinator { get; set; }
    }

    private sealed record CssAttribute(string Name, string? Operator, string? Value);
}
