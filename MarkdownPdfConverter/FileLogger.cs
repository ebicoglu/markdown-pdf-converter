namespace AbpDocsMd2PdfConverter;

public class FileLogger : ILogger
{
    private readonly string _logPath;

    public FileLogger(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, "logs.txt");

        if (File.Exists(_logPath))
        {
            File.Delete(_logPath);
        }
    }

    public void Log(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
    }
}