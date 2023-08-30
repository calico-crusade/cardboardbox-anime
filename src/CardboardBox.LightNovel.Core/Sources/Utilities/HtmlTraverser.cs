namespace CardboardBox.LightNovel.Core.Sources;

public class HtmlTraverser
{
    private readonly HtmlNode? _targetNode;
    public int Index { get; set; } = 0;
    public HtmlNode? Current => _targetNode?.ChildNodes[Index];

    public bool Valid => _targetNode != null && Index < _targetNode.ChildNodes.Count;

    public HtmlTraverser(HtmlDocument rootDoc, string xpath)
    {
        _targetNode = rootDoc.DocumentNode.SelectSingleNode(xpath);
    }

    public HtmlTraverser(HtmlNode? targetNode)
    {
        _targetNode = targetNode;
    }

    public IEnumerable<HtmlNode> EverythingUntil(Func<HtmlNode, bool> predicate)
    {
        if (_targetNode == null) yield break;

        while(Index < _targetNode.ChildNodes.Count)
        {
            var node = Current;
            Index++;
            if (node == null) continue;
            if (predicate(node)) yield break;
            yield return node;
        }
    }

    public IEnumerable<HtmlNode> EverythingUntil(string name) => EverythingUntil(node => node.Name == name);

    public HtmlNode? MoveUntil(Func<HtmlNode, bool> predicate)
    {
        if (_targetNode == null) return null;

        while(Index < _targetNode.ChildNodes.Count)
        {
            var node = Current;
            Index++;
            if (node == null) break;
            if (predicate(node)) return node;
        }

        return null;
    }

    public HtmlNode? MoveUntil(string name) => MoveUntil(node => node.Name == name);

    public IEnumerable<HtmlNode> AfterUntil(Func<HtmlNode, bool> after, Func<HtmlNode, bool> until)
    {
        var af = MoveUntil(after);
        if (af == null) yield break;

        foreach(var node in EverythingUntil(until))
            yield return node;
    }

    public IEnumerable<HtmlNode> EverythingBut(Func<HtmlNode, bool> exclude)
    {
        if (_targetNode == null) yield break;

        while (Index < _targetNode.ChildNodes.Count)
        {
            var node = Current;
            Index++;
            if (node == null || exclude(node)) continue;
            yield return node;
        }
    }

    public HtmlNode? BackUntil(Func<HtmlNode, bool> predicate)
    {
        if (_targetNode == null) return null;

        while(Index > 0)
        {
            var node = Current;
            Index--;
            if (node == null) break;
            if (predicate(node)) return node;
        }

        return null;
    }

    public (HtmlNode? node, int index) UntilOneOf(params Func<HtmlNode, bool>[] preds)
    {
        if (_targetNode == null) return (null, -1);

        while(Index < _targetNode.ChildNodes.Count)
        {
            var node = Current;
            Index++;
            if (node == null) continue;
            var index = Array.FindIndex(preds, x => x(node));
            if (index != -1) return (node, index);
        }

        return (null, -1);
    }
}
