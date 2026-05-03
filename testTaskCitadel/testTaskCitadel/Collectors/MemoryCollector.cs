using System.Runtime.InteropServices;
using testTaskCitadel.Collectors;

namespace testTaskCitadel.Collectors;

public class MemoryCollector : MetricCollector
{
    public override string Name => "Memory";

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public override async Task<Dictionary<string, object>> CollectAsync()
    {
        var result = new Dictionary<string, object>();

        if (OperatingSystem.IsWindows())
        {
            var memStatus = new MEMORYSTATUSEX();
            memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));

            if (GlobalMemoryStatusEx(ref memStatus))
            {
                double totalMB = memStatus.ullTotalPhys / (1024.0 * 1024.0);
                double availMB = memStatus.ullAvailPhys / (1024.0 * 1024.0);
                double usedMB = totalMB - availMB;
                double usagePercent = totalMB > 0 ? (usedMB / totalMB) * 100 : 0;

                result["TotalMB"] = totalMB;
                result["AvailableMB"] = availMB;
                result["UsagePercent"] = usagePercent;
            }
            else
            {
                result["TotalMB"] = 0.0;
                result["AvailableMB"] = 0.0;
                result["UsagePercent"] = 0.0;
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            var meminfo = await File.ReadAllLinesAsync("/proc/meminfo");
            long totalKb = ParseValue(meminfo.First(l => l.StartsWith("MemTotal")));
            long availableKb = ParseValue(meminfo.First(l => l.StartsWith("MemAvailable")));
            long usedKb = totalKb - availableKb;

            result["TotalMB"] = totalKb / 1024f;
            result["AvailableMB"] = availableKb / 1024f;
            result["UsagePercent"] = (usedKb / (float)totalKb) * 100;
        }
        else
        {
            result["TotalMB"] = 0.0;
            result["AvailableMB"] = 0.0;
            result["UsagePercent"] = 0.0;
        }

        return result;
    }

    private long ParseValue(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.Parse(parts[1]);
    }
}