namespace AbpDocsMd2PdfConverter;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var converter = new MdPdfConverter(
            AppConsts.SourceDirectory,
            AppConsts.OutputPdfFile,
            AppConsts.IgnoredContents,
            AppConsts.ShouldAddOriginalMarkdownFilePath,
            AppConsts.LogDirectory,
            AppConsts.ExcludedFolders
        );

        var success = await converter.ConvertAsync();

        if (success)
        {
            Helper.OpenFileViaExplorer(AppConsts.OutputPdfFile);
        }

        Console.ReadKey();
    }
}