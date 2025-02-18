using System.Text.RegularExpressions;

namespace AbpDocsMd2PdfConverter;

public static class Helper
{
    public static string GetGitHubDocumentUrl(string filePath, string markdownContent)
    {
        if (filePath == null || markdownContent == null)
        {
            return String.Empty;
        }


        var localFilePaths = filePath.Split("\\docs\\en");
        if (localFilePaths.Any() && localFilePaths.Length > 1)
        {
            var normalizedFileName = localFilePaths[1].Replace("\\", "/");
            var githubDocumentUrl = "https://github.com/abpframework/abp/blob/dev/docs/en" + normalizedFileName;

            return githubDocumentUrl;

        }

        return String.Empty;
    }

    public static string RemoveDocNavSection(string markdown)
    {
        /*removes the following section:
         ````json
           //[doc-nav]
           {
             "Next": {
               "Name": "Creating the server side",
               "Path": "tutorials/book-store/part-01"
             }
           }
           ````
         */

        markdown = Regex.Replace(
            markdown,
            @"````json\s*//\[doc-nav\][\s\S]*?````",
            string.Empty,
            RegexOptions.Multiline
        );

        return Regex.Replace(
                markdown,
                @"```json\s*//\[doc-nav\][\s\S]*?```",
                string.Empty,
                RegexOptions.Multiline
            );
    }


    public static string ReplaceDocParamsWithThisCombination(
        string markdown,
        Dictionary<string, string>? displayParameters)
    {
        var variables = string.Empty;

        if (displayParameters == null)
        {
            return markdown;
        }

        if (displayParameters.TryGetValue("UI", out var ui))
        {
            variables += $"**UI:** {ui}";
        }

        if (displayParameters.TryGetValue("DB", out var db))
        {
            var database = db == "EF" ? "Entity Framework Core" : db;
            variables += $", **Database:** {database}";
        }

        if (displayParameters.TryGetValue("Tiered", out var tiered))
        {
            variables += $", **Tiered:** {tiered}";
        }

        var replacementText = "* This section is for the project config " + variables;

        // Replace the doc-params JSON block including the code block markers with the formatted text
        return Regex.Replace(
            markdown,
            @"````json\s*//\[doc-params\]\s*{[\s\S]*?}\s*````",
            replacementText,
            RegexOptions.Multiline
        );
    }


    public static void CreateTheOutputDirectoryIfNotExists(string filePath)
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directoryPath))
        {
            return;
        }

        if (Directory.Exists(directoryPath))
        {
            return;
        }

        Directory.CreateDirectory(directoryPath);
    }

    public static List<string> GetMarkdownFiles(string sourceDirectory, string[] excludedFolders)
    {
        return Directory
            .GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories)
            .Where(file => !excludedFolders.Any(folder => file.Contains(Path.Combine(sourceDirectory, folder))))
            .ToList();
    }

    public static bool ShouldIgnoreThisFile(string markdownContent, string[] _ignoredContents)
    {
        foreach (var ignoredText in _ignoredContents)
        {
            if (markdownContent.Contains(ignoredText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static void OpenFileViaExplorer(string filePath)
    {
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            });
    }
}

