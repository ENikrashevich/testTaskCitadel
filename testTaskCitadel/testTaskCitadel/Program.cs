using System.Text.Json;
using testTaskCitadel.Collectors;
using testTaskCitadel.Models;
using testTaskCitadel.Output;
using testTaskCitadel.Collectors;
using testTaskCitadel.Models;
using testTaskCitadel.Output;

namespace SystemMonitor;

class Program
{
    static async Task Main(string[] args)
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        if (!File.Exists(configPath))
        {
            Console.WriteLine("Configuration file appsettings.json not found.");
            return;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

        IOutput output = config.OutputTarget?.ToLower() switch
        {
            "file" => new FileOutput(config.LogFilePath),
            _ => new ConsoleOutput()
        };

        var collectors = new MetricCollector[]
        {
            new CpuCollector(),
            new MemoryCollector(),
            new NetworkCollector()
        };

        Console.WriteLine($"System Monitor started. Interval: {config.IntervalSeconds}s, Output: {config.OutputTarget}");
        Console.WriteLine("Press Ctrl+C to exit.");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var metrics = new List<string> { $"[{timestamp}]" };

            foreach (var collector in collectors)
            {
                try
                {
                    var data = await collector.CollectAsync();
                    var formatted = FormatMetrics(collector.Name, data);
                    metrics.Add(formatted);
                }
                catch (Exception ex)
                {
                    metrics.Add($"{collector.Name}: Error - {ex.Message}");
                }
            }

            await output.WriteAsync(string.Join(" | ", metrics));

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(config.IntervalSeconds), cts.Token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        Console.WriteLine("Monitoring stopped.");
    }

    static string FormatMetrics(string category, Dictionary<string, object> data)
    {
        var parts = data.Select(kv => $"{kv.Key}={FormatValue(kv.Value)}");
        return $"{category}: {string.Join(", ", parts)}";
    }

    static string FormatValue(object value)
    {
        if (value is float f) return f.ToString("F2");
        if (value is double d) return d.ToString("F2");
        if (value is Dictionary<string, float> dict)
            return "{" + string.Join(", ", dict.Select(kv => $"{kv.Key}: {kv.Value:F2}%")) + "}";
        return value.ToString() ?? "?";
    }
}