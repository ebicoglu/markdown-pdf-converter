using iText.Html2pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Element;
using iText.Layout.Font;
using System.Text;

using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Element;
using Markdig;
using iText.Html2pdf;
using System.Text;
using iText.Layout.Font;

namespace AbpSupportQuestionsFetcher;

public class PdfCreator
{
    private readonly ConverterProperties _converterProperties;

    public PdfCreator()
    {
        _converterProperties = new ConverterProperties();
        InitializeFonts();
    }

    private void InitializeFonts()
    {
        var fontProvider = new FontProvider();
        fontProvider.AddStandardPdfFonts();
        fontProvider.AddSystemFonts();
        _converterProperties.SetFontProvider(fontProvider);
    }

    public void AddHtmlContent(Document document, string html)
    {
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



    private static void AddHorizontalLine(Document document)
    {
        document.Add(new LineSeparator(new DottedLine())
            .SetWidth(523)
            .SetMarginTop(10)
            .SetMarginBottom(10)
        );
    }
}