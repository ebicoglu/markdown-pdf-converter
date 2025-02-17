using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using Markdig;
using Scriban;
using Scriban.Runtime;
using iText.Html2pdf;
using iText.Html2pdf.Resolver.Font;
using System.Text;

namespace AbpDocsMd2PdfConverter;

public class MdPdfConverter
{
    private readonly string _sourceDirectory;
    private readonly string _outputPdfPath;
    private readonly string[] _ignoreFilesWhichHave;
    private readonly bool _shouldAddOriginalMarkdownFilePath;
    private readonly ILogger _logger;
    private const int MaxDocumentCount = 30;

    private static readonly string[] ExcludedFolders = [
        "Blog-Posts",
        "Community-Articles",
        ".vscode",
        "_deleted",
        "_resources"
    ];

    public MdPdfConverter(string sourceDirectory, string outputPdfPath, string[] ignoreFilesWhichHave, bool shouldAddOriginalMarkdownFilePath, string logDirectory)
    {
        _sourceDirectory = sourceDirectory;
        _outputPdfPath = outputPdfPath;
        _ignoreFilesWhichHave = ignoreFilesWhichHave;
        _shouldAddOriginalMarkdownFilePath = shouldAddOriginalMarkdownFilePath;
        _logger = new FileLogger(logDirectory);
    }

    public async Task<bool> ConvertAsync()
    {
        try
        {
            var markdownFilePaths = GetMarkdownFiles();
            CreateTheOutputDirectoryIfNotExists(_outputPdfPath);

            using var pdfWriter = new PdfWriter(_outputPdfPath);
            using var pdf = new PdfDocument(pdfWriter);
            using var document = new Document(pdf);

            for (int i = 0; i < markdownFilePaths.Count; i++)
            {
                _logger.Log($"[{(i + 1)} / {markdownFilePaths.Count}] Processing file: {markdownFilePaths[i]}");
                await ProcessNewMarkdownFileAsync(markdownFilePaths[i], document);
            }

            _logger.Log($"PDF generation completed. Output: {_outputPdfPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log($"Error during PDF conversion:\n\r{ex}");
            return false;
        }
    }

    private List<string> GetMarkdownFiles()
    {
        return Directory
            .GetFiles(_sourceDirectory, "*.md", SearchOption.AllDirectories)
            .Where(file => !ExcludedFolders.Any(folder => file.Contains(Path.Combine(_sourceDirectory, folder))))
            .ToList();
    }

    private async Task ProcessNewMarkdownFileAsync(string filePath, Document document)
    {
        try
        {
            var markdownContent = await File.ReadAllTextAsync(filePath);
            if (ShouldIgnoreThisFile(markdownContent))
            {
                return;
            }

            if (_shouldAddOriginalMarkdownFilePath)
            {
                markdownContent = $"_(This document is transformed from {filePath})_" + Environment.NewLine + markdownContent;
            }

            var parameters = ExtractDocParameters(markdownContent);

            if (filePath == "D:\\github\\volosoft\\abp\\docs\\en\\tutorials\\book-store\\index.md")
            {
                Console.WriteLine("test");

                //todo: replace these with values
                //"**{{DB_Value}}**"
                //"**{{UI_Value}}**"
            }

            if (parameters != null)
            {
                var combinations = ParameterCombinator.GenerateCombinations(parameters);
                foreach (var combination in combinations)
                {
                    var renderedMarkdown = await RenderMarkdownWithParameters(markdownContent, combination);
                    renderedMarkdown = ReplaceDocParamsWithThisCombination(renderedMarkdown, combination);

                    AddHtmlContent(document, renderedMarkdown);
                    AddHorizontalLine(document);
                }
            }
            else
            {
                AddHtmlContent(document, markdownContent);
                //  AddHorizontalLine(document);  
            }

            document.Add(new AreaBreak()); //add new document break;
        }
        catch (Exception ex)
        {
            _logger.Log($"*** Error processing file {filePath}: {ex}");
        }
    }

    private bool ShouldIgnoreThisFile(string markdownContent)
    {
        foreach (var ignoredText in _ignoreFilesWhichHave)
        {
            if (markdownContent.Contains(ignoredText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void AddHorizontalLine(Document document)
    {
        var line = new LineSeparator(new DottedLine())
            .SetWidth(523)
            .SetMarginTop(10)
            .SetMarginBottom(10);
        document.Add(line);
    }

    private Dictionary<string, string[]>? ExtractDocParameters(string markdown)
    {
        var match = Regex.Match(markdown, @"//\[doc-params\]\s*({[\s\S]*?})", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        try
        {
            var jsonContent = match.Groups[1].Value;
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(jsonContent);
        }
        catch
        {
            _logger.Log($"Failed to parse doc-params JSON");
            return null;
        }
    }

    private static async Task<string> RenderMarkdownWithParameters(string template, Dictionary<string, string> parameters)
    {
        var scribanTemplate = Template.Parse(template);
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();

        foreach (var param in parameters)
        {
            scriptObject.Add(param.Key, param.Value);
        }

        context.PushGlobal(scriptObject);
        return await scribanTemplate.RenderAsync(context);
    }

    private static string ConvertMarkdownToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        return Markdown.ToHtml(markdown, pipeline);
    }

    private void AddHtmlContent(Document document, string markdown)
    {
        var html = ConvertMarkdownToHtml(markdown);
        var wrappedHtml = $@"
            <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; }}
                        pre {{ background-color: #f6f8fa; padding: 16px; border-radius: 6px; }}
                        code {{ font-family: Consolas, monospace; }}
                    </style>
                </head>
                <body>{html}</body>
            </html>";

        using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(wrappedHtml));
        ConverterProperties converterProperties = new();
        converterProperties.SetFontProvider(new DefaultFontProvider(true, true, true));

        var elements = HtmlConverter.ConvertToElements(htmlStream, converterProperties);
        foreach (var element in elements)
        {
            document.Add((IBlockElement)element);
        }
    }

    private static void CreateTheOutputDirectoryIfNotExists(string filePath)
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


    private static string ReplaceDocParamsWithThisCombination(string markdown, Dictionary<string, string> combination)
    {
        var variables = string.Empty;

        if (combination.TryGetValue("UI", out var ui))
        {
            variables += $"UI: {ui}";
        }

        if (combination.TryGetValue("DB", out var db))
        {
            var database = db == "EF" ? "Entity Framework Core" : db;
            variables += $", Database: {database}";
        }

        if (combination.TryGetValue("Tiered", out var tiered))
        {
            variables += $", Tiered: {tiered}";
        }

        var replacementText = "> *** This section is for this project type " + variables + " ***";
 
        
        // Replace the doc-params JSON block including the code block markers with the formatted text
        return Regex.Replace(
            markdown,
            @"````json\s*//\[doc-params\]\s*{[\s\S]*?}\s*````",
            replacementText,
            RegexOptions.Multiline
        );
    }
}