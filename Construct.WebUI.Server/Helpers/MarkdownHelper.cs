using Markdig;
using Markdig.Extensions.EmphasisExtras;
using System.Text.RegularExpressions;

namespace Construct.WebUI.Server.Helpers;

/// <summary>
/// Helper voor het converteren van markdown naar HTML in Blazor components
/// Ondersteunt: bold, italic, subscript, superscript, custom kleuren
/// </summary>
public static class MarkdownHelper
{
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions() // Tabellen, subscript/superscript, etc.
        .UseEmphasisExtras() // ~~strikethrough~~, ==highlight==
        .Build();

    // Regex voor custom color syntax: {red:text}, {blue:text}, etc.
    private static readonly Regex _colorRegex = new Regex(
        @"\{(?<color>\w+):(?<text>.*?)\}",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    /// <summary>
    /// Pre-process custom syntax (zoals {red:xxx}) voordat Markdig parsing
    /// </summary>
    private static string PreProcessCustomSyntax(string markdown)
    {
        // Vervang {color:text} door <span style="color:color">text</span>
        return _colorRegex.Replace(markdown, match =>
        {
            var color = match.Groups["color"].Value;
            var text = match.Groups["text"].Value;
            return $"<span style=\"color:{color}\">{text}</span>";
        });
    }

    /// <summary>
    /// Converteert markdown string naar HTML
    /// </summary>
    /// <param name="markdown">Markdown tekst zoals "**bold** *italic* {red:warning} H~2~O X^2^"</param>
    /// <returns>HTML string</returns>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Pre-process custom syntax
        markdown = PreProcessCustomSyntax(markdown);

        // Markdig parsing
        return Markdown.ToHtml(markdown, _pipeline);
    }

    /// <summary>
    /// Inline markdown parsing (zonder &lt;p&gt; tags)
    /// </summary>
    public static string ToInlineHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        // Pre-process custom syntax
        markdown = PreProcessCustomSyntax(markdown);

        var html = Markdown.ToHtml(markdown, _pipeline);
        
        // Verwijder wrapping <p> tags
        html = html.Trim();
        if (html.StartsWith("<p>") && html.EndsWith("</p>"))
        {
            html = html.Substring(3, html.Length - 7);
        }
        
        return html;
    }
}

