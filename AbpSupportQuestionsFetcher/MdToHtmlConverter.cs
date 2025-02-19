using Markdig;
using System.Text.RegularExpressions;

namespace AbpSupportQuestionsFetcher;

public class MdToHtmlConverter
{
    public static string Convert(string markdown)
    {
        var websiteRoot = "https://abp.io/support/";

        markdown = LinkfyQuestioNumberHashtags(markdown, websiteRoot);
        var html = Markdown.ToHtml(markdown);

        html = MakeImageSourcesFullLink(html, websiteRoot);

        return html;
    }

    private static string MakeImageSourcesFullLink(string html, string websiteRootAddress)
    {
        try
        {
            websiteRootAddress = websiteRootAddress.TrimEnd('/');

            return Regex.Replace(html, @" src\s*=\s*""(.+?)"" ", match =>
            {
                if (match.Groups[1].Value.StartsWith("/"))
                {
                    return $" src=\"{websiteRootAddress}{match.Groups[1].Value}\" ";
                }

                return match.Value;
            });
        }
        catch
        {
            // ignored
            return html;
        }
    }

    private static string LinkfyQuestioNumberHashtags(string markdownText, string websiteRootAddress)
    {
        try
        {
            websiteRootAddress = websiteRootAddress.EndsWith('/') ? websiteRootAddress : websiteRootAddress + "/";

            return Regex.Replace(markdownText, @"(([\n\r\s])|^)#([1-9]\d*)(\d*\b)", match =>
            {
                if (match.Groups.Count > 4 && long.TryParse(match.Groups[3].Value, out var questionNumber))
                {
                    var value = match.Groups[0].Value;
                    return ReplaceFirst(value, "#" + questionNumber,
                        $"[#{questionNumber}]({websiteRootAddress}QA/Questions/{questionNumber})");
                }

                return match.Value;
            });
        }
        catch
        {
            // ignored
            return markdownText;
        }
    }

    private static string ReplaceFirst(string str, string search, string replace, StringComparison comparisonType = StringComparison.Ordinal)
    {

        var pos = str.IndexOf(search, comparisonType);
        if (pos < 0)
        {
            return str;
        }

        var searchLength = search.Length;
        var replaceLength = replace.Length;
        var newLength = str.Length - searchLength + replaceLength;

        Span<char> buffer = newLength <= 1024 ? stackalloc char[newLength] : new char[newLength];

        // Copy the part of the original string before the search term
        str.AsSpan(0, pos).CopyTo(buffer);

        // Copy the replacement text
        replace.AsSpan().CopyTo(buffer.Slice(pos));

        // Copy the remainder of the original string
        str.AsSpan(pos + searchLength).CopyTo(buffer.Slice(pos + replaceLength));

        return buffer.ToString();
    }
}