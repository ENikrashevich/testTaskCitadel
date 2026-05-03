using testTaskCitadel.Output;

public class FileOutput : IOutput
{
    private readonly string _filePath;
    public FileOutput(string filePath) => _filePath = filePath;

    public async Task WriteAsync(string message)
    {
        await File.AppendAllTextAsync(_filePath, message + Environment.NewLine);
    }
}