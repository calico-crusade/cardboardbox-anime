namespace CardboardBox.LightNovel.Core.Sources;

public interface IPurgeUtils 
{
    string PurgeBadElements(string content);
}

public class PurgeUtils : IPurgeUtils
{
    public string PurgeBadElements(string content)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content);

        PurgeAnchors(doc.DocumentNode);

        return doc.DocumentNode.InnerHtml;
    }

    public void PurgeAnchors(HtmlNode node)
    {
        var anchors = node.SelectNodes("//a");

        foreach (var anchor in anchors)
            PurgeAnchor(anchor);
    }

    public void PurgeAnchor(HtmlNode anchor)
    {
        bool RemoveParents(HtmlNode target, string href)
        {
            var removeParents = new[] { "patreon.com", "ko-fi.com", "paypal.com", "strawpoll.com" };
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
}
