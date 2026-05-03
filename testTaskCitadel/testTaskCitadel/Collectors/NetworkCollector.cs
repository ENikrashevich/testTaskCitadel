using System.Net.NetworkInformation;
using testTaskCitadel.Collectors;

namespace testTaskCitadel.Collectors;

public class NetworkCollector : MetricCollector
{
    public override string Name => "Network";

    private DateTime _lastCheck = DateTime.UtcNow;
    private long _lastReceivedBytes = 0;
    private long _lastSentBytes = 0;

    public override Task<Dictionary<string, object>> CollectAsync()
    {
        var result = new Dictionary<string, object>();
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastCheck).TotalSeconds;

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

        long totalRecv = 0, totalSent = 0;
        foreach (var nic in interfaces)
        {
            var stats = nic.GetIPStatistics();
            totalRecv += stats.BytesReceived;
            totalSent += stats.BytesSent;
        }

        if (_lastReceivedBytes > 0 && elapsedSeconds > 0)
        {
            result["BytesReceivedPerSec"] = (totalRecv - _lastReceivedBytes) / elapsedSeconds;
            result["BytesSentPerSec"] = (totalSent - _lastSentBytes) / elapsedSeconds;
        }
        else
        {
            result["BytesReceivedPerSec"] = 0.0;
            result["BytesSentPerSec"] = 0.0;
        }

        _lastReceivedBytes = totalRecv;
        _lastSentBytes = totalSent;
        _lastCheck = now;

        return Task.FromResult(result);
    }
}