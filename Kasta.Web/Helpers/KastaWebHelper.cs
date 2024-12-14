using System.Collections.ObjectModel;
using Markdig;
using Markdig.Parsers;
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
            new AutolinkInlineParser(),
            new LineBreakInlineParser()
        ]);
        pipeline.BlockParsers.Clear();
        pipeline.BlockParsers.AddRange([
            new ThematicBreakParser(),
            new HeadingBlockParser(),
            new QuoteBlockParser(),

            new FencedCodeBlockParser(),
            new IndentedCodeBlockParser(),
            new ParagraphBlockParser(),
        ]);
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

    public static bool EmbedMedia(string userAgent)
    {
        var robot = EmbedMediaUserAgent.Select(e => e.ToLower()).ToList();

        return robot.Contains(userAgent.ToLower());
    }

    public static bool EmbedLink(string userAgent)
    {
        var robot = EmbedLinkUserAgent.Select(e => e.ToLower()).ToList();

        return robot.Contains(userAgent.ToLower());
    }

    public static BotFeature GetBotFeatures(string userAgent)
    {
        if (EmbedLink(userAgent))
            return BotFeature.EmbedLink;
        if (EmbedMedia(userAgent))
            return BotFeature.EmbedMedia;
        return BotFeature.None;
    }

    public static ReadOnlyCollection<string> EmbedLinkUserAgent => new List<string>()
    {
            "discord",
            // discord image bot
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 11.6; rv:92.0) Gecko/20100101 Firefox/92.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.10; rv:38.0) Gecko/20100101 Firefox/38.0"
    }.AsReadOnly();
    public static ReadOnlyCollection<string> EmbedMediaUserAgent => EmbedLinkUserAgent.Concat([
        "TelegramBot",
        "facebookexternalhit/",
        "Facebot",
        "curl/",
        "WhatsApp/",
        "Slack",
        "Twitterbot",
    ]).Select(e => e.ToLower()).ToList().AsReadOnly();
}

public enum BotFeature
{
    None,
    EmbedLink,
    EmbedMedia,
}