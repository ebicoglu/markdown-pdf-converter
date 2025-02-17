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
using iText.Layout.Font;
using iText.StyledXmlParser.Resolver.Font;
using static iText.IO.Image.Jpeg2000ImageData;

namespace AbpDocsMd2PdfConverter;

public class MdPdfConverter
{
    private readonly string _sourceDirectory;
    private readonly string _outputPdfPath;
    private readonly string[] _ignoreFilesWhichHave;
    private readonly bool _shouldAddOriginalMarkdownFilePath;
    private readonly ILogger _logger;
    private readonly ConverterProperties _converterProperties;
    private readonly DisplayParameter _displayParameters;

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
        _converterProperties = new ConverterProperties();

        InitializeFonts();
        _displayParameters = DisplayParameter.Build();
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

            if (parameters != null)
            {
                var combinations = ParameterCombinator.GenerateCombinations(parameters);
                foreach (var combination in combinations)
                {
                    var displayParameters = BuildDisplayParameters(combination);
                    var renderedMarkdown = await RenderMarkdownWithParameters(markdownContent, combination, displayParameters);
                    renderedMarkdown = ReplaceDocParamsWithThisCombination(renderedMarkdown, displayParameters);
                    renderedMarkdown = RemoveDocNavSection(renderedMarkdown);

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

    private static void AddHorizontalLine(Document document)
    {
        document.Add(new LineSeparator(new DottedLine())
            .SetWidth(523)
            .SetMarginTop(10)
            .SetMarginBottom(10)
        );
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

    private async Task<string> RenderMarkdownWithParameters(
        string template,
        Dictionary<string, string> parameters,
        Dictionary<string, string> displayParameters)
    {
        var scribanTemplate = Template.Parse(template);
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();

        foreach (var param in parameters)
        {
            scriptObject.Add(param.Key, param.Value);
        }

        foreach (var displayParam in displayParameters)
        {
            scriptObject.Add(displayParam.Key + "_Value", displayParam.Value);
        }

        context.PushGlobal(scriptObject);
        return await scribanTemplate.RenderAsync(context);
    }

    private Dictionary<string, string> BuildDisplayParameters(Dictionary<string, string> combinations)
    {
        var displayParameters = new Dictionary<string, string>();

        foreach (var combination in combinations)
        {
            var selectedDisplayValues = _displayParameters.Values.Where(x => x.Name == combination.Key).FirstOrDefault();
            if (selectedDisplayValues != null)
            {
                displayParameters[combination.Key] = selectedDisplayValues.Values[combination.Value];
            }
        }

        return displayParameters;
    }

    private static string ConvertMarkdownToHtml(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        return Markdown.ToHtml(markdown, pipeline);
    }

    private void InitializeFonts()
    {
        var fontProvider = new FontProvider();
        fontProvider.AddStandardPdfFonts();
        fontProvider.AddSystemFonts();
        _converterProperties.SetFontProvider(fontProvider);
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
        var elements = HtmlConverter.ConvertToElements(htmlStream, _converterProperties);

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


    private static string ReplaceDocParamsWithThisCombination(string markdown, Dictionary<string, string> displayParameters)
    {
        var variables = string.Empty;

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

    private static string RemoveDocNavSection(string markdown)
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

        return Regex.Replace(
            markdown,
            @"````json\s*//\[doc-nav\][\s\S]*?````",
            string.Empty,
            RegexOptions.Multiline
        );
    }

}