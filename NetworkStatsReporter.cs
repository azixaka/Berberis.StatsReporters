using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

namespace Berberis.StatsReporters;

public sealed class NetworkStatsReporter
{
    private long _lastTicks;
    private long _lastRcvBytes;
    private long _lastSndBytes;

    private readonly object _syncObj = new();

    private readonly List<Dictionary<string, string>> _networkInfo;

    private readonly NetworkInterface _networkInterface;

    public NetworkStatsReporter(string networkInterfaceName)
    {
        var nis = NetworkInterface.GetAllNetworkInterfaces();

        _networkInfo = new List<Dictionary<string, string>>(
            nis.Select(ni =>
            {
                string name = null;
                try { name = ni.Name; } catch { }

                string desc = null;
                try { desc = ni.Description; } catch { }

                string type = null;
                try { type = ni.NetworkInterfaceType.ToString(); } catch { }

                string speed = null;
                try { speed = ni.Speed.ToString(); } catch { }

                string status = null;
                try { status = ni.OperationalStatus.ToString(); } catch { }

                string multicast = null;
                try { multicast = ni.SupportsMulticast.ToString(); } catch { }

                return new Dictionary<string, string>
                    {
                            {"Name", name},
                            {"Description", desc},
                            {"Type", type},
                            {"Speed", speed},
                            {"Status", status},
                            {"Multicast", multicast}
                    };
            }));

        _networkInterface = nis.FirstOrDefault(ni => ni.Name == networkInterfaceName);

        _lastTicks = Stopwatch.GetTimestamp();
    }

    public IReadOnlyList<IReadOnlyDictionary<string, string>> NetworkInfo => _networkInfo;

    public NetworkStats GetStats()
    {
        lock (_syncObj)
        {
            var ticks = Stopwatch.GetTimestamp();
            float timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency * 1000;
            _lastTicks = ticks;

            if (_networkInterface != null)
            {
                // the LinuxIPInterfaceStatistics implementation relies on https://github.com/dotnet/corefx/blob/master/src/System.Net.NetworkInformation/src/System/Net/NetworkInformation/StringParsingHelpers.Statistics.cs
                // which artificially limits all values read from Linux network stats table file to UInt64. Couldn't figure out why but it means stats stop after 4bln bytes sent/received.

                var stats = _networkInterface.GetIPStatistics();

                var rcvd = stats.BytesReceived;
                var intervalRcvBytes = (rcvd - _lastRcvBytes) * 1000 / timePassed;
                _lastRcvBytes = rcvd;

                var sent = stats.BytesSent;
                var intervalSndBytes = (sent - _lastSndBytes) * 1000 / timePassed;
                _lastSndBytes = sent;

                return new NetworkStats(timePassed, _lastRcvBytes, _lastSndBytes, intervalRcvBytes,
                    intervalSndBytes);
            }

            return new NetworkStats(timePassed, -1, -1, 0, 0);
        }
    }
}
