using Markdig;
using ReverseMarkdown;

namespace CardboardBox.LightNovel.Core;

public interface IMarkdownService
{
    string ToHtml(string markdown);
    string ToMarkdown(string html);
}

public class MarkdownService : IMarkdownService
{
    public string ToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        return Markdown.ToHtml(markdown, pipeline);
    }

    public string ToMarkdown(string html)
    {
        var config = new Config
        {
            GithubFlavored = true,
            RemoveComments = true,
            SmartHrefHandling = true,
            UnknownTags = Config.UnknownTagsOption.PassThrough,
        };
        return new Converter(config)
            .Convert(html);
    }
}
