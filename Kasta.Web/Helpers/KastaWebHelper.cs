using Markdig;
using Markdig.Parsers.Inlines;

namespace Kasta.Web.Helpers;

public static class KastaWebHelper
{
    /// <summary>
    /// Convert the markdown <paramref name="content"/> provided, and convert it to HTML. Will only do bold, italic, and links.
    /// </summary>
    public static string MarkdownToHtmlBasic(string content)
    {
        var pipeline = new MarkdownPipelineBuilder();
        pipeline.InlineParsers.Clear();
        pipeline.InlineParsers.AddRange([
            new LinkInlineParser(),
            new EmphasisInlineParser(),
            new CodeInlineParser(),
            new LineBreakInlineParser()
        ]);
        pipeline.BlockParsers.Clear();
        pipeline.Extensions.Clear();

        var result = Markdown.ToHtml(content, pipeline.Build());
        return result;
    }

    /// <summary>
    /// Convert the markdown <paramref name="content"/> provided into HTML using the default settings in <see cref="MarkdownPipelineBuilder"/>
    /// </summary>
    public static string MarkdownToHtml(string content)
    {
        var pipeline = new MarkdownPipelineBuilder();
        
        var result = Markdown.ToHtml(content, pipeline.Build());
        return result;
    }
}