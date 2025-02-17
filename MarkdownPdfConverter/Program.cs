using System.Diagnostics;

namespace AbpDocsMd2PdfConverter;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var sourceDirectory = @"D:\github\volosoft\abp\docs\en";
        var outputPdfFile = $"D:\\temp\\abp-docs-{DateTime.Now:yyyy-MM-dd}.pdf";
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        bool shouldAddOriginalMarkdownFilePath = true;
        var ignoreFilesWhichHave = new[]
        {
            "document is planned to be written later",
            "document has been moved",
            "feature is still under development",
        };

        var converter = new MdPdfConverter(sourceDirectory, outputPdfFile, ignoreFilesWhichHave, shouldAddOriginalMarkdownFilePath, logDirectory);
        var success = await converter.ConvertAsync();

        if (success)
        {
            Process.Start(new ProcessStartInfo(outputPdfFile) { UseShellExecute = true });
        }

        Console.ReadKey();
    }
}