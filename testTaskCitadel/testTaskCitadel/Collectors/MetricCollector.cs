namespace testTaskCitadel.Collectors;

public abstract class MetricCollector
{
    public abstract string Name { get; }
    public abstract Task<Dictionary<string, object>> CollectAsync();
}