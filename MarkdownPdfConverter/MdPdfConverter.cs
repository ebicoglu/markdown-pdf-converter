using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using Markdig;
using iText.Html2pdf;
using System.Text;
using iText.Layout.Font;

namespace AbpDocsMd2PdfConverter;

public class MdPdfConverter
{
    private readonly string _sourceDirectory;
    private readonly string _outputPdfPath;
    private readonly string[] _ignoredContents;
    private readonly bool _shouldAddOriginalMarkdownFilePath;
    private readonly string[] _excludedFolders;
    private readonly ILogger _logger;
    private readonly ConverterProperties _converterProperties;
    private readonly DisplayParameter _displayParameters;


    public MdPdfConverter(string sourceDirectory,
        string outputPdfPath,
        string[] ignoredContents,
        bool shouldAddOriginalMarkdownFilePath,
        string logDirectory,
        string[] excludedFolders)
    {
        _sourceDirectory = sourceDirectory;
        _outputPdfPath = outputPdfPath;
        _ignoredContents = ignoredContents;
        _shouldAddOriginalMarkdownFilePath = shouldAddOriginalMarkdownFilePath;
        _excludedFolders = excludedFolders;
        _logger = new FileLogger(logDirectory);
        _converterProperties = new ConverterProperties();
        InitializeFonts();
        _displayParameters = DisplayParameter.Build();
    }

    public async Task<bool> ConvertAsync()
    {
        try
        {
            var markdownFilePaths = Helper.GetMarkdownFiles(_sourceDirectory, _excludedFolders);
            Helper.CreateTheOutputDirectoryIfNotExists(_outputPdfPath);

            using var pdfWriter = new PdfWriter(_outputPdfPath);
            using var pdf = new PdfDocument(pdfWriter);
            using var document = new Document(pdf);

            for (int i = 0; i < markdownFilePaths.Count; i++)
            {
                _logger.Log($"[{(i + 1)} / {markdownFilePaths.Count}] {markdownFilePaths[i]}");
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

    private async Task ProcessNewMarkdownFileAsync(string filePath, Document document)
    {
        try
        {
            var markdownContent = await File.ReadAllTextAsync(filePath);
            if (Helper.ShouldIgnoreThisFile(markdownContent, _ignoredContents))
            {
                return;
            }

            if (_shouldAddOriginalMarkdownFilePath)
            {
                var gitHubUrl = Helper.GetGitHubDocumentUrl(filePath, markdownContent);
                markdownContent = $"_(The original document is [here]({gitHubUrl}))_" + Environment.NewLine + markdownContent;
            }

            var parameters = ExtractDocParameters(markdownContent);
            var combinations = ParameterCombinator.Generate(parameters);

            if (combinations.Length > 0)
            {
                foreach (var combination in combinations)
                {
                    BuildPdfDocument(document, markdownContent, true, combination);
                }
            }
            else
            {
                BuildPdfDocument(document, markdownContent, false);
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"*** Error processing file {filePath}: {ex}");
        }
    }

    private async Task<string> FillDocumentParameters(Dictionary<string, string>? combination, string markdownContent)
    {
        var displayParameters = BuildDisplayParameters(combination);
        var renderedMarkdown = await ScribanRenderer.Render(markdownContent, combination, displayParameters);
        renderedMarkdown = Helper.ReplaceDocParamsWithThisCombination(renderedMarkdown, displayParameters);
        return renderedMarkdown;
    }


    private static void AddHorizontalLine(Document document)
    {
        document.Add(new LineSeparator(new DottedLine())
            .SetWidth(523)
            .SetMarginTop(10)
            .SetMarginBottom(10)
        );
    }

    private Dictionary<string, string[]> ExtractDocParameters(string markdown)
    {
        var match = Regex.Match(markdown, @"//\[doc-params\]\s*({[\s\S]*?})", RegexOptions.Multiline);
        if (!match.Success)
        {
            return new Dictionary<string, string[]>();
        }

        try
        {
            var jsonContent = match.Groups[1].Value;
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(jsonContent);
        }
        catch
        {
            _logger.Log($"Failed to parse doc-params JSON");
            return new Dictionary<string, string[]>();
        }
    }

    private Dictionary<string, string>? BuildDisplayParameters(Dictionary<string, string>? combinations)
    {
        if (combinations == null)
        {
            return null;
        }

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


    private async Task BuildPdfDocument(Document document, string markdown, bool shouldAddHorizontalLine, Dictionary<string, string>? combination = null)
    {
        markdown = await FillDocumentParameters(combination, markdown);

        markdown = Helper.RemoveDocNavSection(markdown);

        AddHtmlContent(document, markdown);

        if (shouldAddHorizontalLine)
        {
            AddHorizontalLine(document);
        }

        document.Add(new AreaBreak()); //add new document break;
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




}