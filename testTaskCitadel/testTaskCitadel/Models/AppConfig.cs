namespace testTaskCitadel.Models;

public class AppConfig
{
    public int IntervalSeconds { get; set; } = 2;
    public string OutputTarget { get; set; } = "Console";
    public string LogFilePath { get; set; } = "metrics.log";
}