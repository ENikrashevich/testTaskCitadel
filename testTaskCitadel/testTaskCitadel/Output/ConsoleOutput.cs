using testTaskCitadel.Output;

public class ConsoleOutput : IOutput
{
    public Task WriteAsync(string message)
    {
        Console.WriteLine(message);
        return Task.CompletedTask;
    }
}