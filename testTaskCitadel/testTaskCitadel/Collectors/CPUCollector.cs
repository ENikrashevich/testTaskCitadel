using System.Diagnostics;
using testTaskCitadel.Collectors;

namespace testTaskCitadel.Collectors;

public class CpuCollector : MetricCollector
{
    public override string Name => "CPU";

#if WINDOWS
    private PerformanceCounter? _totalCpuCounter;
    private PerformanceCounter[]? _coreCounters;
#endif

    private DateTime _lastCpuTime = DateTime.UtcNow;
    private long _lastTotalTime = 0;
    private long _lastIdleTime = 0;
    private Dictionary<int, (long user, long nice, long system, long idle)> _lastCoreStats = new();

    public CpuCollector()
    {
#if WINDOWS
        try
        {
            _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            int coreCount = Environment.ProcessorCount;
            _coreCounters = new PerformanceCounter[coreCount];
            for (int i = 0; i < coreCount; i++)
            {
                _coreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", $"{i}");
            }
        }
        catch {}
#endif
    }

    public override async Task<Dictionary<string, object>> CollectAsync()
    {
        var result = new Dictionary<string, object>();

#if WINDOWS
        if (_totalCpuCounter != null)
        {
            result["TotalUsage"] = _totalCpuCounter.NextValue();
            var coreUsages = new Dictionary<string, float>();
            if (_coreCounters != null)
            {
                for (int i = 0; i < _coreCounters.Length; i++)
                {
                    coreUsages[$"Core{i}"] = _coreCounters[i].NextValue();
                }
            }
            result["PerCoreUsage"] = coreUsages;
            return await Task.FromResult(result);
        }
#endif

        if (OperatingSystem.IsLinux())
        {
            var lines = await File.ReadAllLinesAsync("/proc/stat");
            var cpuLine = lines.FirstOrDefault(l => l.StartsWith("cpu "));
            if (cpuLine != null)
            {
                var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long user = long.Parse(parts[1]);
                long nice = long.Parse(parts[2]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
                long irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
                long softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;
                long steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;

                long totalIdle = idle + iowait;
                long totalNonIdle = user + nice + system + irq + softirq + steal;
                long total = totalIdle + totalNonIdle;

                if (_lastTotalTime > 0 && _lastIdleTime > 0)
                {
                    long totalDelta = total - _lastTotalTime;
                    long idleDelta = totalIdle - _lastIdleTime;
                    float usage = (float)(totalDelta - idleDelta) / totalDelta * 100;
                    result["TotalUsage"] = usage;
                }
                else
                {
                    result["TotalUsage"] = 0f;
                }

                _lastTotalTime = total;
                _lastIdleTime = totalIdle;
            }

            var coreUsages = new Dictionary<string, float>();
            var coreLines = lines.Where(l => l.StartsWith("cpu") && l[3] >= '0' && l[3] <= '9');
            foreach (var line in coreLines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string coreId = parts[0][3..];
                long user = long.Parse(parts[1]);
                long nice = long.Parse(parts[2]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
                long irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
                long softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;
                long steal = parts.Length > 8 ? long.Parse(parts[8]) : 0;

                long totalIdle = idle + iowait;
                long totalNonIdle = user + nice + system + irq + softirq + steal;
                long total = totalIdle + totalNonIdle;

                if (_lastCoreStats.TryGetValue(int.Parse(coreId), out var last))
                {
                    long totalDelta = total - (last.user + last.nice + last.system + last.idle);
                    long idleDelta = totalIdle - last.idle;
                    float usage = (float)(totalDelta - idleDelta) / totalDelta * 100;
                    coreUsages[$"Core{coreId}"] = usage;
                }
                else
                {
                    coreUsages[$"Core{coreId}"] = 0f;
                }

                _lastCoreStats[int.Parse(coreId)] = (user, nice, system, totalIdle);
            }
            result["PerCoreUsage"] = coreUsages;
        }
        else if (OperatingSystem.IsWindows())
        {
            result["TotalUsage"] = 0f;
            result["PerCoreUsage"] = new Dictionary<string, float>();
        }

        return await Task.FromResult(result);
    }
}