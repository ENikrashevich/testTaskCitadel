namespace testTaskCitadel.Output;

public interface IOutput
{
    Task WriteAsync(string message);
}