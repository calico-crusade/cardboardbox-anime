namespace CardboardBox.LightNovel.Core.Sources;

public interface IPurgeUtils 
{
    string PurgeBadElements(string content);

    IEnumerable<HtmlNode> Flatten(HtmlDocument doc);

    IEnumerable<HtmlNode> Flatten(HtmlNode node);
}

public class PurgeUtils : IPurgeUtils
{
    public string PurgeBadElements(string content)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        PurgeAnchors(doc.DocumentNode);
        PurgeTips(doc.DocumentNode);

        return doc.DocumentNode.InnerHtml;
    }

    public void PurgeAnchors(HtmlNode node)
    {
        var anchors = node.SelectNodes("//a");
        if (anchors is null) return;

        foreach (var anchor in anchors)
            PurgeAnchor(anchor);
    }

    public void PurgeAnchor(HtmlNode anchor)
    {
        bool RemoveParents(HtmlNode target, string href)
        {
            var removeParents = new[] { "patreon.com", "ko-fi.com", "paypal.com", "strawpoll.com", "aliexpress.com" };
            foreach (var h in removeParents)
            {
                if (!href.Contains(h)) continue;

                target.ParentNode.Remove();
                return true;
            }

            return false;
        }

        bool RemoveTarget(HtmlNode target, string href)
        {
            var remove = new[] { "/cdn-cgi/l/email-protection", "#respond" };
            foreach (var h in remove)
            {
                if (!href.Contains(h)) continue;

                target.Remove();
                return true;
            }

            return false;
        }

        bool Reposition(HtmlNode target, string href)
        {
            var reposition = new[] { "javascript:void(0)", "#fn-", "#easy-footnote" };
            foreach (var h in reposition)
            {
                if (!href.Contains(h)) continue;

                var inner = target.InnerHtml;

                target.ParentNode.InsertBefore(HtmlNode.CreateNode(inner), target);
                target.Remove();
                return true;
            }

            return false;
        }

        var actions = new[]
        {
            RemoveParents,
            RemoveTarget,
            Reposition
        };

        var href = anchor.GetAttributeValue("href", "").ToLower();
        if (string.IsNullOrEmpty(href))
        {
            if (!string.IsNullOrWhiteSpace(anchor.InnerHtml))
                anchor.ParentNode.InsertBefore(HtmlNode.CreateNode(anchor.InnerHtml), anchor);
            anchor.Remove();
            return;
        }

        if (actions.Any(t => t(anchor, href))) return;

        var nyxSets = new[] { "next >>", "<< previous" };
        if (nyxSets.Any(t => anchor.InnerText.HTMLDecode().Trim().ToLower() == t))
        {
            HandleNyxShare(anchor);
            return;
        }
    }

    public void PurgeTips(HtmlNode node)
    {
        string[] inners = ["Tip: You can use left"];

        var tips = node.SelectNodes("//code");
        if (tips is null) return;

        foreach (var item in tips)
        {
            var inner = item.InnerText.HTMLDecode().Trim();
            if (!inners.Any(t => inner.Contains(t, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            item.ParentNode.Remove();
        }
    }

    public void HandleNyxShare(HtmlNode anchor)
    {
        var parent = GetFirstNode(anchor);

        while(parent.NextSibling != null)
            parent.NextSibling.Remove();

        parent.Remove();
    }

    public HtmlNode GetFirstNode(HtmlNode node)
    {
        if (node.ParentNode == null || 
            node.ParentNode.Name == "#document") 
            return node;

        return GetFirstNode(node.ParentNode);
    }

    public IEnumerable<HtmlNode> Flatten(HtmlDocument doc)
    {
        return Flatten(doc.DocumentNode);
    }

    public IEnumerable<HtmlNode> Flatten(HtmlNode node)
    {
        string[] passThrough = ["p", "img", "strong", "b", "i", "h1", "h2", "h3", "h4", "h5", "h6"];
        string[] barred = ["script", "style", "iframe", "noscript", "object", "embed", "input"];

        if (barred.Contains(node.Name))
            yield break;

        if (!node.HasChildNodes)
        {
            yield return node;
            yield break;
        }

        if (node.ChildNodes.Count == 1 && node.ChildNodes[0].NodeType == HtmlNodeType.Text)
        {
            yield return node;
            yield break;
        }

        if (passThrough.Contains(node.Name))
        {
            yield return node;
            yield break;
        }

        foreach (var child in node.ChildNodes)
        {
            foreach (var n in Flatten(child))
                yield return n;
        }
    }
}
