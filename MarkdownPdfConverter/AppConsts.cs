namespace AbpDocsMd2PdfConverter;

public static class AppConsts
{
    public const string SourceDirectory = @"D:\github\volosoft\abp\docs\en"; //@"D:\temp";  

    public static string OutputPdfFile = $"D:\\temp\\abp-docs-{DateTime.Now:yyyy-MM-dd}.pdf";

    public static readonly string[] ExcludedFolders = [
        "Blog-Posts",
        "Community-Articles",
        ".vscode",
        "_deleted",
        "_resources"
    ];

    public static readonly string[] IgnoredContents = [
        "document is planned to be written later",
        "document has been moved",
        "feature is still under development",
    ];

    public const bool ShouldAddOriginalMarkdownFilePath = true;

    public static string LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
}